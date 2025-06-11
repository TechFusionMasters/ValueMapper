using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace SimpleMapperUtility
{
    public static class SimpleMapper
    {
        private class PropertyMapping<TSrc, TDst>
        {
            public string Name { get; }
            public Func<TSrc, object> Getter { get; }
            public Action<TDst, object> Setter { get; }
            public Func<object, object> Converter { get; }
            public Type DestType { get; }

            public PropertyMapping(string name, Func<TSrc, object> getter, Action<TDst, object> setter, Func<object, object> converter, Type destType)
            {
                Name = name;
                Getter = getter;
                Setter = setter;
                Converter = converter;
                DestType = destType;
            }
        }

        // Permanently cache common mappings, no expiration needed
        private static readonly ConcurrentDictionary<string, Delegate> _mappingCache = new ConcurrentDictionary<string, Delegate>();

        // Cache property information
        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _propertyCache = new ConcurrentDictionary<Type, PropertyInfo[]>();
        private static readonly ConcurrentDictionary<Type, HashSet<string>> _typeIgnoredCache = new ConcurrentDictionary<Type, HashSet<string>>();

        // Cache constructed types to avoid repeated typeof(Nullable<>) operations
        private static readonly ConcurrentDictionary<Type, Type> _nullableTypeCache = new ConcurrentDictionary<Type, Type>();

        // Cache numeric type flags for faster type checking
        private static readonly ConcurrentDictionary<Type, bool> _numericTypeCache = new ConcurrentDictionary<Type, bool>();

        // Pre-cache commonly used types
        private static readonly Type StringType = typeof(string);
        private static readonly MethodInfo ChangeTypeMethod = typeof(Convert).GetMethod("ChangeType", new[] { typeof(object), typeof(Type) });

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TDestination Map<TSource, TDestination>(TSource source, ISet<string> ignoredProperties = null)
            where TDestination : new()
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var map = GetMappingFunc<TSource, TDestination>();
            var dest = new TDestination();
            map(source, dest, ignoredProperties);
            return dest;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<TDestination> MapList<TSource, TDestination>(IEnumerable<TSource> list, ISet<string> ignoredProperties = null)
            where TDestination : new()
        {
            if (list == null) throw new ArgumentNullException(nameof(list));

            var array = list as TSource[] ?? list.ToArray();
            if (array.Length == 0) return new List<TDestination>();

            // Use sequential approach for small collections - parallelism has overhead
            const int PARALLEL_THRESHOLD = 500;
            const int MAX_PARALLELISM = 8;

            if (array.Length < PARALLEL_THRESHOLD)
            {
                var result = new List<TDestination>(array.Length);
                var map = GetMappingFunc<TSource, TDestination>();

                // Use direct iteration to avoid LINQ overhead
                for (int i = 0; i < array.Length; i++)
                {
                    var dest = new TDestination();
                    map(array[i], dest, ignoredProperties);
                    result.Add(dest);
                }
                return result;
            }

            // For larger collections, use parallelism with optimized settings
            return array.AsParallel()
                      .WithDegreeOfParallelism(Math.Min(Environment.ProcessorCount, MAX_PARALLELISM))
                      .Select(x => Map<TSource, TDestination>(x, ignoredProperties))
                      .ToList();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Action<TSource, TDestination, ISet<string>> GetMappingFunc<TSource, TDestination>()
        {
            var key = typeof(TSource).FullName + "->" + typeof(TDestination).FullName;

            // Get or create the mapping function and cast to the appropriate delegate type
            return (Action<TSource, TDestination, ISet<string>>)_mappingCache.GetOrAdd(key, _ => BuildMap<TSource, TDestination>());
        }

        private static Action<TSource, TDestination, ISet<string>> BuildMap<TSource, TDestination>()
        {
            var srcType = typeof(TSource);
            var dstType = typeof(TDestination);

            // Get source properties, cache by name for faster lookup
            var srcProps = GetTypeProperties(srcType)
                              .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

            var dstProps = GetTypeProperties(dstType);
            var alwaysIgnore = GetTypeIgnored(dstType);

            var maps = new List<PropertyMapping<TSource, TDestination>>();

            foreach (var dstProp in dstProps)
            {
                if (alwaysIgnore.Contains(dstProp.Name) || !dstProp.CanWrite)
                    continue;

                // Determine source property
                PropertyInfo srcProp = null;
                var attrDst = dstProp.GetCustomAttribute<SimpleMapperMappingAttribute>(true);

                if (attrDst != null)
                {
                    // Attribute on destination: use specified source name
                    srcProps.TryGetValue(attrDst.SourcePropertyName, out srcProp);
                }
                else if (!srcProps.TryGetValue(dstProp.Name, out srcProp))
                {
                    // No match by name: look for attribute on source property mapping to this dst
                    srcProp = srcProps.Values.FirstOrDefault(sp =>
                        sp.GetCustomAttribute<SimpleMapperMappingAttribute>(true)?.SourcePropertyName
                        .Equals(dstProp.Name, StringComparison.OrdinalIgnoreCase) == true);
                }

                if (srcProp == null)
                    continue;

                // Create converter function
                var converter = CreateConverter(srcProp.PropertyType, dstProp.PropertyType);
                if (converter == null)
                    continue;

                // Create optimized getter
                var srcParam = Expression.Parameter(typeof(TSource), "s");
                var getter = Expression.Lambda<Func<TSource, object>>(
                    Expression.Convert(Expression.Property(srcParam, srcProp), typeof(object)),
                    srcParam
                ).Compile();

                // Create optimized setter
                var dstParam = Expression.Parameter(typeof(TDestination), "d");
                var valParam = Expression.Parameter(typeof(object), "v");
                var setterExpr = Expression.Call(
                    dstParam,
                    dstProp.GetSetMethod(true),
                    Expression.Convert(valParam, dstProp.PropertyType)
                );
                var setter = Expression.Lambda<Action<TDestination, object>>(
                    setterExpr,
                    dstParam,
                    valParam
                ).Compile();

                maps.Add(new PropertyMapping<TSource, TDestination>(
                    dstProp.Name,
                    getter,
                    setter,
                    converter,
                    dstProp.PropertyType
                ));
            }

            // Return optimized mapping function
            return (src, dst, ignored) =>
            {
                foreach (var map in maps)
                {
                    if (ignored != null && ignored.Contains(map.Name))
                        continue;

                    var srcValue = map.Getter(src);
                    if (srcValue == null && IsNonNullable(map.DestType))
                        continue;

                    var convertedValue = map.Converter(srcValue);
                    map.Setter(dst, convertedValue);
                }
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsNonNullable(Type t)
            => t.IsValueType && GetNullableUnderlyingType(t) == null;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static object GetDefault(Type t)
            => t.IsValueType ? Activator.CreateInstance(t) : null;

        private static Func<object, object> CreateConverter(Type src, Type dst)
        {
            // Same type, no conversion needed
            if (src == dst) return v => v;

            // Handle nullable destination types
            var dstNullable = GetNullableUnderlyingType(dst);
            if (dstNullable != null)
            {
                var innerConverter = CreateConverter(src, dstNullable);
                return v => v == null ? null : innerConverter(v);
            }

            // String to enum conversion - allow exception to be thrown for invalid values
            if (dst.IsEnum && src == StringType)
            {
                return v => {
                    if (v == null) return GetDefault(dst);
                    // Let this throw ArgumentException for invalid values
                    return Enum.Parse(dst, (string)v, true);
                };
            }

            // Enum to string conversion
            if (src.IsEnum && dst == StringType)
            {
                return v => v?.ToString();
            }

            // Enum to numeric conversion
            if (src.IsEnum && IsNumeric(dst))
            {
                return v => {
                    try
                    {
                        return v == null ? GetDefault(dst) : Convert.ChangeType(v, dst);
                    }
                    catch
                    {
                        return GetDefault(dst);
                    }
                };
            }

            // Numeric to enum conversion
            if (IsNumeric(src) && dst.IsEnum)
            {
                return v => {
                    try
                    {
                        return v == null ? GetDefault(dst) : Enum.ToObject(dst, v);
                    }
                    catch
                    {
                        return GetDefault(dst);
                    }
                };
            }

            // Numeric and string conversions
            if ((IsNumeric(src) || src == StringType) && (IsNumeric(dst) || dst == StringType))
            {
                return v => {
                    try
                    {
                        return v == null ? GetDefault(dst) : Convert.ChangeType(v, dst);
                    }
                    catch
                    {
                        return GetDefault(dst);
                    }
                };
            }

            // Try general conversion as a last resort
            return v => {
                try
                {
                    return v == null ? GetDefault(dst) : Convert.ChangeType(v, dst);
                }
                catch
                {
                    return GetDefault(dst);
                }
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool CanConvert(Type src, Type dst)
        {
            if (src == dst) return true;

            var dstNullable = GetNullableUnderlyingType(dst);
            if (dstNullable != null)
                return CanConvert(src, dstNullable);

            if (dst == StringType) return true; // Anything can be converted to string
            if (dst.IsEnum)
            {
                // Can convert string or numeric to enum
                return src == StringType || IsNumeric(src);
            }
            if (src.IsEnum)
            {
                // Can convert enum to string or numeric
                return dst == StringType || IsNumeric(dst);
            }

            // Can convert between numeric types or string
            return (IsNumeric(src) || src == StringType) && (IsNumeric(dst) || dst == StringType);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static PropertyInfo[] GetTypeProperties(Type t)
            => _propertyCache.GetOrAdd(t, _ => t.GetProperties(BindingFlags.Public | BindingFlags.Instance));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static HashSet<string> GetTypeIgnored(Type t)
            => _typeIgnoredCache.GetOrAdd(t, _ => new HashSet<string>(
                   GetTypeProperties(t)
                   .Where(p => p.GetCustomAttribute<SimpleMapperIgnoreAttribute>(true) != null)
                   .Select(p => p.Name)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Type GetNullableUnderlyingType(Type t)
            => _nullableTypeCache.GetOrAdd(t, _ => Nullable.GetUnderlyingType(t));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsNumeric(Type t)
            => _numericTypeCache.GetOrAdd(t, _ => {
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
                    default:
                        return false;
                }
            });

        public static void ClearCaches()
        {
            _mappingCache.Clear();
            _propertyCache.Clear();
            _typeIgnoredCache.Clear();
            _nullableTypeCache.Clear();
            _numericTypeCache.Clear();
        }
    }

}
