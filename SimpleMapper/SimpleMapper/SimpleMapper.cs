using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Linq;
using System.Reflection;

namespace SimpleMapperUtility
{
    public static class SimpleMapper
    {
        private class ExpiringDelegate
        {
            public Delegate Func;
            public DateTime LastAccess;
        }

        private static readonly ConcurrentDictionary<string, ExpiringDelegate> _mappingCache = new ConcurrentDictionary<string, ExpiringDelegate>();
        private static readonly TimeSpan MappingCacheTTL = TimeSpan.FromHours(1);

        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _propertyCache = new ConcurrentDictionary<Type, PropertyInfo[]>();
        private static readonly ConcurrentDictionary<Type, HashSet<string>> _typeIgnoredCache = new ConcurrentDictionary<Type, HashSet<string>>();

        public static TDestination Map<TSource, TDestination>(TSource source, ISet<string> ignoredProperties = null)
            where TDestination : new()
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var map = GetMappingFunc<TSource, TDestination>();
            var dest = new TDestination();
            map(source, dest, ignoredProperties);
            return dest;
        }

        public static List<TDestination> MapList<TSource, TDestination>(IEnumerable<TSource> list, ISet<string> ignoredProperties = null)
            where TDestination : new()
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            var array = list as TSource[] ?? list.ToArray();
            const int PAR = 100, MAX = 8;
            if (array.Length < PAR)
                return array.Select(x => Map<TSource, TDestination>(x, ignoredProperties)).ToList();
            return array.AsParallel()
                        .WithDegreeOfParallelism(Math.Min(Environment.ProcessorCount, MAX))
                        .Select(x => Map<TSource, TDestination>(x, ignoredProperties))
                        .ToList();
        }

        private static Action<TSource, TDestination, ISet<string>> GetMappingFunc<TSource, TDestination>()
        {
            var key = $"{typeof(TSource).FullName}->{typeof(TDestination).FullName}";
            EvictOld();
            var entry = _mappingCache.GetOrAdd(key, _ => new ExpiringDelegate
            {
                Func = BuildMap<TSource, TDestination>(),
                LastAccess = DateTime.UtcNow
            });
            entry.LastAccess = DateTime.UtcNow;
            return (Action<TSource, TDestination, ISet<string>>)entry.Func;
        }

        private static void EvictOld()
        {
            var now = DateTime.UtcNow;
            foreach (var kv in _mappingCache.ToArray())
                if (now - kv.Value.LastAccess > MappingCacheTTL)
                    _mappingCache.TryRemove(kv.Key, out _);
        }

        private static Action<TSource, TDestination, ISet<string>> BuildMap<TSource, TDestination>()
        {
            var srcProps = GetTypeProperties(typeof(TSource))
                              .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);
            var dstProps = GetTypeProperties(typeof(TDestination));
            var alwaysIgnore = GetTypeIgnored(typeof(TDestination));

            var maps = new List<PropertyMapping<TSource, TDestination>>();

            foreach (var dst in dstProps)
            {
                if (alwaysIgnore.Contains(dst.Name))
                    continue;

                // Determine source property
                PropertyInfo src = null;
                var attrDst = dst.GetCustomAttribute<SimpleMapperMappingAttribute>(true);
                if (attrDst != null)
                {
                    // Attribute on destination: use specified source name
                    srcProps.TryGetValue(attrDst.SourcePropertyName, out src);
                }
                else if (!srcProps.TryGetValue(dst.Name, out src))
                {
                    // No match by name: look for attribute on source property mapping to this dst
                    src = srcProps.Values.FirstOrDefault(sp =>
                        sp.GetCustomAttribute<SimpleMapperMappingAttribute>(true)?.SourcePropertyName
                        .Equals(dst.Name, StringComparison.OrdinalIgnoreCase) == true);
                }

                if (src == null || !dst.CanWrite)
                    continue;

                var conv = CreateConverter(src.PropertyType, dst.PropertyType);
                if (conv == null)
                    continue;

                // compile getter
                var parSrc = Expression.Parameter(typeof(TSource), "s");
                var getter = Expression.Lambda<Func<TSource, object>>(
                    Expression.Convert(Expression.Property(parSrc, src), typeof(object)), parSrc).Compile();

                // compile setter
                var parDst = Expression.Parameter(typeof(TDestination), "d");
                var parVal = Expression.Parameter(typeof(object), "v");
                var setterCall = Expression.Call(
                    parDst,
                    dst.GetSetMethod(true),
                    Expression.Convert(parVal, dst.PropertyType));
                var setter = Expression.Lambda<Action<TDestination, object>>(setterCall, parDst, parVal).Compile();

                maps.Add(new PropertyMapping<TSource, TDestination>(dst.Name, getter, setter, conv));
            }

            return (s, d, ignored) =>
            {
                foreach (var m in maps)
                {
                    if (ignored != null && ignored.Contains(m.Name))
                        continue;

                    var raw = m.Getter(s);
                    if (raw == null && IsNonNullable(m.DestType))
                        continue;

                    var converted = m.Converter(raw);
                    m.Setter(d, converted);
                }
            };
        }

        private static PropertyInfo[] GetTypeProperties(Type t)
            => _propertyCache.GetOrAdd(t, _ => t.GetProperties(BindingFlags.Public | BindingFlags.Instance));

        private static HashSet<string> GetTypeIgnored(Type t)
            => _typeIgnoredCache.GetOrAdd(t, _ => new HashSet<string>(
                   GetTypeProperties(t)
                   .Where(p => p.GetCustomAttribute<SimpleMapperIgnoreAttribute>(true) != null)
                   .Select(p => p.Name)));

        private static Func<object, object> CreateConverter(Type src, Type dst)
        {
            if (src == dst) return v => v;
            var nullable = Nullable.GetUnderlyingType(dst);
            if (nullable != null)
            {
                var inner = CreateConverter(src, nullable);
                return v => v == null ? null : inner(v);
            }
            if (dst == typeof(string) && src.IsEnum) return v => v?.ToString();
            if (dst.IsEnum && src == typeof(string)) return v => v == null ? Activator.CreateInstance(dst) : Enum.Parse(dst, (string)v, true);
            if (dst.IsEnum && IsNumeric(src)) return v => v == null ? Activator.CreateInstance(dst) : Enum.ToObject(dst, v);
            if ((IsNumeric(src) || src == typeof(string)) && (IsNumeric(dst) || dst == typeof(string)))
                return v =>
                {
                    try { return v == null ? GetDefault(dst) : Convert.ChangeType(v, dst); }
                    catch { return GetDefault(dst); }
                };
            var change = typeof(Convert).GetMethod("ChangeType", new[] { typeof(object), typeof(Type) });
            if (change != null)
                return v =>
                {
                    try { return v == null ? GetDefault(dst) : Convert.ChangeType(v, dst); }
                    catch { return GetDefault(dst); }
                };
            return null;
        }

        private static bool IsNumeric(Type t)
        {
            switch (Type.GetTypeCode(t))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                    return true;
                default: return false;
            }
        }

        private static bool IsNonNullable(Type t)
            => t.IsValueType && Nullable.GetUnderlyingType(t) == null;

        private static object GetDefault(Type t)
            => t.IsValueType ? Activator.CreateInstance(t) : null;

        public static void ClearCaches()
        {
            _mappingCache.Clear();
            _propertyCache.Clear();
            _typeIgnoredCache.Clear();
        }

        private class PropertyMapping<TSrc, TDst>
        {
            public string Name { get; }
            public Func<TSrc, object> Getter { get; }
            public Action<TDst, object> Setter { get; }
            public Func<object, object> Converter { get; }
            public Type DestType { get; }
            public PropertyMapping(string name, Func<TSrc, object> getter, Action<TDst, object> setter, Func<object, object> converter)
            {
                Name = name;
                Getter = getter;
                Setter = setter;
                Converter = converter;
                DestType = setter.Method.GetParameters()[1].ParameterType;
            }
        }
    }
}
