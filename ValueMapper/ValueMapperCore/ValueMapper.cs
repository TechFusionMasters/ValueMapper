using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using ValueMapperUtility.Attribute;
// Internal implementation detail - hidden from API


namespace ValueMapperUtility
{
    /// <summary>
    /// Static generic cache for mapping functions.
    /// Each TSource/TDestination pair gets its own static instance,
    /// and the CLR guarantees thread-safe initialization.
    /// </summary>
    internal static class StaticMapper<TSource, TDestination> where TDestination : new()
    {
        // Static field to hold the mapping function - not readonly so it can be updated
        public static Action<TSource, TDestination, ISet<string>> Map;

        // Flag to track if this mapper has been cleared
        private static volatile bool _isCleared = false;

        // Static constructor runs exactly once per TSource/TDestination pair
        static StaticMapper()
        {
            Map = ValueMapper.BuildMap<TSource, TDestination>();
        }

        // Called by ValueMapper.ClearCaches to mark this mapper as cleared
        public static void MarkCleared()
        {
            _isCleared = true;
        }

        // Gets the map, rebuilding it if it has been cleared
        public static Action<TSource, TDestination, ISet<string>> GetMap()
        {
            if (_isCleared)
            {
                // Create a new mapping function and update the static field
                Map = ValueMapper.BuildMap<TSource, TDestination>();
                _isCleared = false;
            }

            return Map;
        }
    }

    /// <summary>
    /// Static generic cache for type converters.
    /// Each source/destination type pair gets its own static instance.
    /// </summary>
    internal static class Converter<TSource, TDest>
    {
        // Static field to hold the conversion function
        public static readonly Func<TSource, TDest> Convert;

        // Static constructor runs exactly once per type pair
        static Converter()
        {
            Convert = ValueMapper.BuildConverter<TSource, TDest>();
        }
    }

    /// <summary>
    /// Optimized mapper that efficiently converts between object types
    /// </summary>
    public static class ValueMapper
    {
        // Struct to avoid allocating for property mappings
        private struct MappingEntry<TSrc, TDst>
        {
            public readonly Delegate Getter;
            public readonly Delegate Setter;
            public readonly string Name;
            public readonly Type DestType;
            public readonly bool IsNullable;

            public MappingEntry(string name, Delegate getter, Delegate setter, Type destType, bool isNullable)
            {
                Name = name;
                Getter = getter;
                Setter = setter;
                DestType = destType;
                IsNullable = isNullable;
            }
        }

        // Property and ignored properties cache
        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _propertyCache = new ConcurrentDictionary<Type, PropertyInfo[]>();
        private static readonly ConcurrentDictionary<Type, HashSet<string>> _typeIgnoredCache = new ConcurrentDictionary<Type, HashSet<string>>();

        // Pre-cache commonly used types
        private static readonly Type StringType = typeof(string);
        private static readonly Type ObjectType = typeof(object);

        // Common numeric types pre-cached
        private static readonly HashSet<Type> NumericTypes = new HashSet<Type>
        {
            typeof(byte), typeof(sbyte), typeof(short), typeof(ushort),
            typeof(int), typeof(uint), typeof(long), typeof(ulong),
            typeof(float), typeof(double), typeof(decimal)
        };

        // Parallel mapping thresholds
        private const int PARALLEL_THRESHOLD = 200;
        private const int MAX_DEGREE_OF_PARALLELISM = 8;

        /// <summary>
        /// Fast-path for mapping objects of the same type - zero overhead
        /// </summary>
        public static T Map<T>(T source) => source;

        /// <summary>
        /// Maps a single source object to a new destination object
        /// </summary>
        public static TDestination Map<TSource, TDestination>(TSource source, ISet<string> ignoredProperties = null)
            where TDestination : new()
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            // Fast path for identical types when semantics allow
            if (typeof(TSource) == typeof(TDestination) && ignoredProperties == null)
            {
                return (TDestination)(object)source;
            }

            // Get mapping function from static cache - guaranteed thread-safe initialization
            var mapFunc = StaticMapper<TSource, TDestination>.GetMap();

            var dest = new TDestination();
            mapFunc(source, dest, ignoredProperties);
            return dest;
        }

        /// <summary>
        /// Maps a collection of source objects to a list of destination objects
        /// </summary>
        public static List<TDestination> MapList<TSource, TDestination>(IEnumerable<TSource> list, ISet<string> ignoredProperties = null)
            where TDestination : new()
        {
            if (list == null) throw new ArgumentNullException(nameof(list));

            var array = list as TSource[] ?? list.ToArray();

            // Fast path for identical types when semantics allow
            if (typeof(TSource) == typeof(TDestination) && ignoredProperties == null)
            {
                return array.Cast<TDestination>().ToList();
            }

            // Optimization: pre-allocate result list
            var result = new List<TDestination>(array.Length);

            // Get mapping function from static cache
            var mapFunc = StaticMapper<TSource, TDestination>.GetMap();

            // For small lists, use sequential processing
            if (array.Length < PARALLEL_THRESHOLD)
            {
                foreach (var item in array)
                {
                    var dest = new TDestination();
                    mapFunc(item, dest, ignoredProperties);
                    result.Add(dest);
                }
                return result;
            }

            // For larger lists, use optimized parallel processing with custom partitioning
            // Using a partitioner that respects order
            var destinations = new TDestination[array.Length];

            Parallel.For(0, array.Length,
                new ParallelOptions { MaxDegreeOfParallelism = Math.Min(Environment.ProcessorCount, MAX_DEGREE_OF_PARALLELISM) },
                i => {
                    destinations[i] = new TDestination();
                    mapFunc(array[i], destinations[i], ignoredProperties);
                });

            result.AddRange(destinations);
            return result;
        }

        /// <summary>
        /// Pre-warms the mapping cache for a specific TSource/TDestination pair
        /// </summary>
        public static void PreWarmMapping<TSource, TDestination>()
            where TDestination : new()
        {
            // Just access the static field to trigger type initialization
            var _ = StaticMapper<TSource, TDestination>.Map;
        }

        /// <summary>
        /// Clears the mapping cache for a specific TSource/TDestination pair
        /// </summary>
        public static void Clear<TSource, TDestination>() where TDestination : new()
        {
            StaticMapper<TSource, TDestination>.MarkCleared();
        }

        /// <summary>
        /// Clears all caches (property, ignored properties)
        /// </summary>
        public static void ClearCaches()
        {
            _propertyCache.Clear();
            _typeIgnoredCache.Clear();
        }

        // This method is internal and called by StaticMapper's static constructor
        internal static Action<TSource, TDestination, ISet<string>> BuildMap<TSource, TDestination>()
        {
            var srcProps = GetTypeProperties(typeof(TSource))
                              .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);
            var dstProps = GetTypeProperties(typeof(TDestination));
            var alwaysIgnore = GetTypeIgnored(typeof(TDestination));

            // Use array for faster iteration
            var mappings = new List<MappingEntry<TSource, TDestination>>();

            foreach (var dst in dstProps)
            {
                if (alwaysIgnore.Contains(dst.Name) || !dst.CanWrite)
                    continue;

                // Determine source property
                PropertyInfo src = null;
                var attrDst = dst.GetCustomAttribute<ValueMapperMappingAttribute>(true);
                if (attrDst != null)
                {
                    // Attribute on destination: use specified source name
                    srcProps.TryGetValue(attrDst.SourcePropertyName, out src);
                }
                else if (!srcProps.TryGetValue(dst.Name, out src))
                {
                    // No match by name: look for attribute on source property mapping to this dst
                    src = srcProps.Values.FirstOrDefault(sp =>
                        sp.GetCustomAttribute<ValueMapperMappingAttribute>(true)?.SourcePropertyName
                        .Equals(dst.Name, StringComparison.OrdinalIgnoreCase) == true);
                }

                if (src == null)
                    continue;

                // Create strongly-typed getter and setter with appropriate conversion
                var srcType = src.PropertyType;
                var dstType = dst.PropertyType;

                // Special case for string to enum mapping
                if (dstType.IsEnum && srcType == typeof(string))
                {
                    // Create a getter for the source string property
                    var srcParam = Expression.Parameter(typeof(TSource), "s");
                    var srcPropExpr = Expression.Property(srcParam, src);
                    var getterExpr = Expression.Lambda<Func<TSource, string>>(srcPropExpr, srcParam);
                    var getter = getterExpr.Compile();

                    // Create a setter for the destination enum property
                    var dstParam = Expression.Parameter(typeof(TDestination), "d");
                    var valParam = Expression.Parameter(typeof(object), "v");
                    var setterExpr = Expression.Lambda<Action<TDestination, object>>(
                        Expression.Call(
                            dstParam,
                            dst.GetSetMethod(true),
                            Expression.Convert(valParam, dstType)
                        ),
                        dstParam,
                        valParam
                    );
                    var setter = setterExpr.Compile();

                    // Create a function that converts string to enum and handles exceptions
                    Func<TSource, object> convertingGetter = source =>
                    {
                        string value = getter(source);

                        if (string.IsNullOrEmpty(value))
                            return Enum.ToObject(dstType, 0); // Default enum value

                        // This will throw ArgumentException for invalid values
                        return Enum.Parse(dstType, value, true);
                    };

                    // Add the mapping entry
                    mappings.Add(new MappingEntry<TSource, TDestination>(
                        dst.Name,
                        convertingGetter,
                        setter,
                        dstType,
                        false
                    ));

                    continue; // Skip the normal conversion process for this property
                }

                // Special case for collections
                if (IsCollection(srcType) && IsCollection(dstType))
                {
                    // Handle List<T> by creating a new instance and copying items
                    var mapping1 = CreateCollectionMapping<TSource, TDestination>(dst.Name, src, dst, srcType, dstType);
                    if (mapping1.Getter != null && mapping1.Setter != null)
                    {
                        mappings.Add(mapping1);
                        continue;
                    }
                }

                // Normal property mapping
                var converter = CreateConverter(srcType, dstType);
                if (converter == null)
                    continue;

                // Compile getter
                var parSrc = Expression.Parameter(typeof(TSource), "s");
                var propExpr = Expression.Property(parSrc, src);

                // Compile setter
                var parDst = Expression.Parameter(typeof(TDestination), "d");
                var parVal = Expression.Parameter(dstType, "v");
                var setterCall = Expression.Call(parDst, dst.GetSetMethod(true), parVal);

                // Get or create the conversion function
                var isNullable = !IsNonNullable(dstType);

                // Create strongly-typed property mapping via dynamic delegate creation
                var mapping = CreatePropertyMapping<TSource, TDestination>(
                    dst.Name,
                    src,
                    dst,
                    srcType,
                    dstType,
                    parSrc,
                    propExpr,
                    parDst,
                    parVal,
                    setterCall,
                    converter,
                    isNullable
                );

                mappings.Add(mapping);
            }

            // Convert to array for faster iteration
            var mappingsArray = mappings.ToArray();

            return (source, dest, ignored) =>
            {
                for (int i = 0; i < mappingsArray.Length; i++)
                {
                    ref readonly var mapping = ref mappingsArray[i];

                    if (ignored != null && ignored.Contains(mapping.Name))
                        continue;

                    // Use type-specific delegates to avoid boxing/unboxing
                    InvokeMapping(mapping, source, dest);
                }
            };
        }

        // Create mapping for collection properties
        private static MappingEntry<TSource, TDestination> CreateCollectionMapping<TSource, TDestination>(
            string name, PropertyInfo srcProp, PropertyInfo dstProp, Type srcType, Type dstType)
        {
            // For List<T> types, we need to create a new list and copy items
            if (dstType.IsGenericType && dstType.GetGenericTypeDefinition() == typeof(List<>))
            {
                var elementType = dstType.GetGenericArguments()[0];

                // Create a copy delegate
                var copyMethod = typeof(ValueMapper).GetMethod(
                    nameof(CopyList),
                    BindingFlags.NonPublic | BindingFlags.Static)
                    .MakeGenericMethod(elementType);

                // Create a getter that clones the source collection
                var srcParam = Expression.Parameter(typeof(TSource), "s");
                var srcPropExpr = Expression.Property(srcParam, srcProp);

                var getterBody = Expression.Call(
                    null,
                    copyMethod,
                    Expression.Convert(srcPropExpr, typeof(IEnumerable<>).MakeGenericType(elementType))
                );

                var getterExpr = Expression.Lambda<Func<TSource, object>>(
                    Expression.Convert(getterBody, typeof(object)),
                    srcParam
                );
                var getter = getterExpr.Compile();

                // Create a setter for the collection
                var dstParam = Expression.Parameter(typeof(TDestination), "d");
                var valueParam = Expression.Parameter(typeof(object), "v");
                var setterExpr = Expression.Lambda<Action<TDestination, object>>(
                    Expression.Call(
                        dstParam,
                        dstProp.GetSetMethod(true),
                        Expression.Convert(valueParam, dstType)
                    ),
                    dstParam, valueParam
                );
                var setter = setterExpr.Compile();

                return new MappingEntry<TSource, TDestination>(
                    name, getter, setter, dstType, true);
            }

            // For other collection types, just copy the reference
            var srcParamDirect = Expression.Parameter(typeof(TSource), "s");
            var srcPropExprDirect = Expression.Property(srcParamDirect, srcProp);

            var getterExprDirect = Expression.Lambda<Func<TSource, object>>(
                Expression.Convert(srcPropExprDirect, typeof(object)),
                srcParamDirect
            );
            var getterDirect = getterExprDirect.Compile();

            // Create a setter that assigns the collection reference
            var dstParamDirect = Expression.Parameter(typeof(TDestination), "d");
            var valueParamDirect = Expression.Parameter(typeof(object), "v");
            var setterExprDirect = Expression.Lambda<Action<TDestination, object>>(
                Expression.Call(
                    dstParamDirect,
                    dstProp.GetSetMethod(true),
                    Expression.Convert(valueParamDirect, dstType)
                ),
                dstParamDirect, valueParamDirect
            );
            var setterDirect = setterExprDirect.Compile();

            return new MappingEntry<TSource, TDestination>(
                name, getterDirect, setterDirect, dstType, true);
        }

        // Helper method to copy a list, always creating a new instance
        private static List<T> CopyList<T>(IEnumerable<T> source)
        {
            if (source == null)
            {
                return new List<T>();
            }
            return new List<T>(source);
        }

        // Helper method to invoke the mapping delegates with their correct types
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void InvokeMapping<TSource, TDestination>(
            in MappingEntry<TSource, TDestination> mapping,
            TSource source,
            TDestination dest)
        {
            // For enums and complex types, use the object-based mapping
            if (mapping.DestType.IsEnum || Type.GetTypeCode(mapping.DestType) == TypeCode.Object)
            {
                InvokeMappingObject(mapping, source, dest);
                return;
            }

            // For primitive types, use type-specific mapping
            switch (Type.GetTypeCode(mapping.DestType))
            {
                case TypeCode.Boolean:
                    InvokeMappingTyped<TSource, TDestination, bool>(mapping, source, dest);
                    break;
                case TypeCode.Byte:
                    InvokeMappingTyped<TSource, TDestination, byte>(mapping, source, dest);
                    break;
                case TypeCode.Char:
                    InvokeMappingTyped<TSource, TDestination, char>(mapping, source, dest);
                    break;
                case TypeCode.DateTime:
                    InvokeMappingTyped<TSource, TDestination, DateTime>(mapping, source, dest);
                    break;
                case TypeCode.Decimal:
                    InvokeMappingTyped<TSource, TDestination, decimal>(mapping, source, dest);
                    break;
                case TypeCode.Double:
                    InvokeMappingTyped<TSource, TDestination, double>(mapping, source, dest);
                    break;
                case TypeCode.Int16:
                    InvokeMappingTyped<TSource, TDestination, short>(mapping, source, dest);
                    break;
                case TypeCode.Int32:
                    InvokeMappingTyped<TSource, TDestination, int>(mapping, source, dest);
                    break;
                case TypeCode.Int64:
                    InvokeMappingTyped<TSource, TDestination, long>(mapping, source, dest);
                    break;
                case TypeCode.SByte:
                    InvokeMappingTyped<TSource, TDestination, sbyte>(mapping, source, dest);
                    break;
                case TypeCode.Single:
                    InvokeMappingTyped<TSource, TDestination, float>(mapping, source, dest);
                    break;
                case TypeCode.String:
                    InvokeMappingTyped<TSource, TDestination, string>(mapping, source, dest);
                    break;
                case TypeCode.UInt16:
                    InvokeMappingTyped<TSource, TDestination, ushort>(mapping, source, dest);
                    break;
                case TypeCode.UInt32:
                    InvokeMappingTyped<TSource, TDestination, uint>(mapping, source, dest);
                    break;
                case TypeCode.UInt64:
                    InvokeMappingTyped<TSource, TDestination, ulong>(mapping, source, dest);
                    break;
                default:
                    // For all other types, use the object-based mapping
                    InvokeMappingObject(mapping, source, dest);
                    break;
            }
        }

        // Typed mapping for primitive types to avoid boxing
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void InvokeMappingTyped<TSource, TDestination, T>(
            in MappingEntry<TSource, TDestination> mapping,
            TSource source,
            TDestination dest)
        {
            try
            {
                var getter = (Func<TSource, T>)mapping.Getter;
                var value = getter(source);

                // Skip null values for non-nullable destination types
                if (value == null && !mapping.IsNullable)
                    return;

                var setter = (Action<TDestination, T>)mapping.Setter;
                setter(dest, value);
            }
            catch (ArgumentException)
            {
                // Rethrow ArgumentException (needed for enum conversion)
                throw;
            }
            catch
            {
                // Silently fail on other conversion errors
            }
        }

        // Object-based mapping for complex types
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void InvokeMappingObject<TSource, TDestination>(
            in MappingEntry<TSource, TDestination> mapping,
            TSource source,
            TDestination dest)
        {
            try
            {
                var getter = (Func<TSource, object>)mapping.Getter;
                var value = getter(source);

                // Skip null values for non-nullable destination types
                if (value == null && !mapping.IsNullable)
                    return;

                var setter = (Action<TDestination, object>)mapping.Setter;
                setter(dest, value);
            }
            catch (ArgumentException)
            {
                // Rethrow ArgumentException (needed for enum conversion)
                throw;
            }
            catch
            {
                // Silently fail on other conversion errors
            }
        }

        // Creates a strongly-typed property mapping
        private static MappingEntry<TSource, TDestination> CreatePropertyMapping<TSource, TDestination>(
            string name,
            PropertyInfo src,
            PropertyInfo dst,
            Type srcType,
            Type dstType,
            ParameterExpression parSrc,
            MemberExpression propExpr,
            ParameterExpression parDst,
            ParameterExpression parVal,
            MethodCallExpression setterCall,
            Delegate converter,
            bool isNullable)
        {
            Delegate getter;
            Delegate setter;

            // Try to create a strongly-typed mapping with conversion
            if (srcType == dstType)
            {
                // No conversion needed, direct property mapping
                var getterExpr = Expression.Lambda(
                    Expression.GetFuncType(typeof(TSource), dstType),
                    propExpr,
                    parSrc);

                var setterExpr = Expression.Lambda(
                    Expression.GetActionType(typeof(TDestination), dstType),
                    setterCall,
                    parDst,
                    parVal);

                getter = getterExpr.Compile();
                setter = setterExpr.Compile();
            }
            else
            {
                // Need conversion, we'll handle it through our cached converters
                var getterExpr = Expression.Lambda(
                    Expression.GetFuncType(typeof(TSource), srcType),
                    propExpr,
                    parSrc);

                var getterTyped = getterExpr.Compile();

                // Create a getter that applies the conversion
                var getterMethod = typeof(ValueMapper).GetMethod(nameof(CreateConvertingGetter),
                    BindingFlags.NonPublic | BindingFlags.Static);

                var genericGetterMethod = getterMethod.MakeGenericMethod(
                    typeof(TSource), srcType, dstType);

                getter = (Delegate)genericGetterMethod.Invoke(null, new object[] { getterTyped, converter });

                // Create the setter
                var setterExpr = Expression.Lambda(
                    Expression.GetActionType(typeof(TDestination), dstType),
                    setterCall,
                    parDst,
                    parVal);

                setter = setterExpr.Compile();
            }

            return new MappingEntry<TSource, TDestination>(
                name, getter, setter, dstType, isNullable);
        }

        // Creates a getter that applies a conversion function
        private static Func<TSource, TDest> CreateConvertingGetter<TSource, TSourceProp, TDest>(
            Func<TSource, TSourceProp> getter,
            Delegate converter)
        {
            var typedConverter = (Func<TSourceProp, TDest>)converter;
            return source => typedConverter(getter(source));
        }

        // Builds a converter between two types using static generic caching
        internal static Func<TSource, TDest> BuildConverter<TSource, TDest>()
        {
            Type srcType = typeof(TSource);
            Type dstType = typeof(TDest);

            // Identity converter for same types
            if (srcType == dstType)
            {
                return x => (TDest)(object)x;
            }

            // Handle nullable destination types
            Type dstNullable = Nullable.GetUnderlyingType(dstType);
            if (dstNullable != null)
            {
                // Create an appropriate nullable wrapper
                return BuildNullableConverter<TSource, TDest>(dstNullable);
            }

            // Handle special cases
            if (dstType.IsEnum && srcType == typeof(string))
            {
                // String to enum conversion
                return (TSource source) =>
                {
                    string value = source as string;
                    if (string.IsNullOrEmpty(value))
                        return (TDest)Enum.ToObject(dstType, 0);
                    return (TDest)Enum.Parse(dstType, value, true);
                };
            }
            else if (dstType.IsEnum && IsNumeric(srcType))
            {
                // Numeric to enum conversion
                return (TSource source) =>
                {
                    if (source == null)
                        return default;
                    return (TDest)Enum.ToObject(dstType, source);
                };
            }
            else if (srcType.IsEnum && dstType == typeof(string))
            {
                // Enum to string conversion
                return (TSource source) =>
                {
                    if (source == null)
                        return default;
                    return (TDest)(object)source.ToString();
                };
            }
            else if ((IsNumeric(srcType) || srcType == typeof(string)) &&
                     (IsNumeric(dstType) || dstType == typeof(string)))
            {
                // Numeric or string conversion
                return BuildNumericOrStringConverter<TSource, TDest>();
            }

            // General conversion via Convert.ChangeType
            try
            {
                return BuildChangeTypeConverter<TSource, TDest>();
            }
            catch
            {
                return default;
            }
        }

        // Builds a nullable converter
        private static Func<TSource, TDest> BuildNullableConverter<TSource, TDest>(Type underlyingType)
        {
            // Create a converter to the underlying type
            var method = typeof(ValueMapper).GetMethod(
                nameof(BuildNullableConverterInternal),
                BindingFlags.NonPublic | BindingFlags.Static);

            var genericMethod = method.MakeGenericMethod(typeof(TSource), underlyingType);
            return (Func<TSource, TDest>)genericMethod.Invoke(null, null);
        }

        // Helper for building nullable converters
        private static Func<TSource, Nullable<TDest>> BuildNullableConverterInternal<TSource, TDest>()
            where TDest : struct
        {
            // Get a converter to the underlying type
            var innerConverter = Converter<TSource, TDest>.Convert;

            return x => x == null ? (TDest?)null : innerConverter(x);
        }

        // Builds a numeric or string converter
        private static Func<TSource, TDest> BuildNumericOrStringConverter<TSource, TDest>()
        {
            Type srcType = typeof(TSource);
            Type dstType = typeof(TDest);

            // Find the appropriate Convert method
            var methodName = "To" + dstType.Name;
            var method = typeof(Convert).GetMethod(methodName, new[] { srcType });

            if (method == null)
                return default;

            return (TSource source) =>
            {
                try
                {
                    if (source == null)
                        return default;
                    return (TDest)method.Invoke(null, new object[] { source });
                }
                catch
                {
                    return default;
                }
            };
        }

        // Builds a general converter using Convert.ChangeType
        private static Func<TSource, TDest> BuildChangeTypeConverter<TSource, TDest>()
        {
            Type dstType = typeof(TDest);

            return (TSource source) =>
            {
                try
                {
                    if (source == null)
                        return default;
                    return (TDest)Convert.ChangeType(source, dstType);
                }
                catch
                {
                    return default;
                }
            };
        }

        // Helper for creating a converter
        private static Delegate CreateConverter(Type srcType, Type dstType)
        {
            // Use reflection to create a converter using our static generic Converter<,> class
            var method = typeof(ValueMapper).GetMethod(
                nameof(GetGenericConverter),
                BindingFlags.NonPublic | BindingFlags.Static);

            var genericMethod = method.MakeGenericMethod(srcType, dstType);
            return (Delegate)genericMethod.Invoke(null, null);
        }

        // Helper to get a converter from the static generic cache
        private static Func<TSource, TDest> GetGenericConverter<TSource, TDest>()
        {
            return Converter<TSource, TDest>.Convert;
        }

        private static PropertyInfo[] GetTypeProperties(Type t)
            => _propertyCache.GetOrAdd(t, _ => t.GetProperties(BindingFlags.Public | BindingFlags.Instance));

        private static HashSet<string> GetTypeIgnored(Type t)
            => _typeIgnoredCache.GetOrAdd(t, _ => new HashSet<string>(
                   GetTypeProperties(t)
                   .Where(p => p.GetCustomAttribute<ValueMapperIgnoreAttribute>(true) != null)
                   .Select(p => p.Name)));

        private static bool IsNumeric(Type t)
        {
            if (t == null)
                return false;

            Type underlyingType = Nullable.GetUnderlyingType(t) ?? t;
            return NumericTypes.Contains(underlyingType);
        }

        private static bool IsNonNullable(Type t)
            => t != null && t.IsValueType && Nullable.GetUnderlyingType(t) == null;

        // Check if a type is a collection
        private static bool IsCollection(Type type)
        {
            if (type == typeof(string)) return false;

            return
                type.IsArray ||
                (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)) ||
                typeof(System.Collections.IEnumerable).IsAssignableFrom(type);
        }

        // Get the element type of a collection
        private static Type GetElementType(Type collectionType)
        {
            if (collectionType.IsArray)
                return collectionType.GetElementType();

            if (collectionType.IsGenericType)
                return collectionType.GetGenericArguments()[0];

            return typeof(object);
        }
    }
}
// Internal implementation detail - hidden from API


