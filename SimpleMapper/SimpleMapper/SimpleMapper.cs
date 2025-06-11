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
        private static readonly ConcurrentDictionary<string, Delegate> _mappingCache = new ConcurrentDictionary<string, Delegate>();
        private static readonly ConcurrentDictionary<string, Dictionary<string, string>> _customMappings = new ConcurrentDictionary<string, Dictionary<string, string>>();
        private static readonly ConcurrentDictionary<string, HashSet<string>> _ignoredPropertiesCache = new ConcurrentDictionary<string, HashSet<string>>();

        /// <summary>
        /// Maps the properties of the source object to a new instance of the destination type.
        /// Supports ignoring properties dynamically via the ignoredProperties parameter.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <typeparam name="TDestination">The destination type.</typeparam>
        /// <param name="source">The source object to map.</param>
        /// <param name="ignoredProperties">A set of property names to ignore during mapping.</param>
        /// <returns>A new instance of TDestination with mapped properties.</returns>
        public static TDestination Map<TSource, TDestination>(TSource source, HashSet<string> ignoredProperties = null)
            where TDestination : new()
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            string cacheKey = $"{typeof(TSource).FullName}->{typeof(TDestination).FullName}";
            if (ignoredProperties != null)
            {
                string ignoredPropertiesKey = string.Join(",", ignoredProperties.OrderBy(p => p));
                cacheKey += $"|Ignored:{ignoredPropertiesKey}";
            }

            if (!_mappingCache.TryGetValue(cacheKey, out var cachedMapping))
            {
                var mappingFunction = CreateMappingFunction<TSource, TDestination>(ignoredProperties);
                _mappingCache[cacheKey] = mappingFunction;
                cachedMapping = mappingFunction;
            }

            var mapFunction = (Func<TSource, TDestination>)cachedMapping;
            return mapFunction(source);
        }

        /// <summary>
        /// Maps the properties of a list of source objects to a list of destination type instances.
        /// Supports ignoring properties dynamically via the ignoredProperties parameter.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <typeparam name="TDestination">The destination type.</typeparam>
        /// <param name="sourceList">The list of source objects to map.</param>
        /// <param name="ignoredProperties">A set of property names to ignore during mapping.</param>
        /// <returns>A list of mapped destination type instances.</returns>
        public static IList<TDestination> MapList<TSource, TDestination>(IEnumerable<TSource> sourceList, HashSet<string> ignoredProperties = null)
            where TDestination : new()
        {
            if (sourceList == null)
                throw new ArgumentNullException(nameof(sourceList));

            return sourceList.AsParallel().Select(source => Map<TSource, TDestination>(source, ignoredProperties)).ToList();
        }

        /// <summary>
        /// Creates a mapping function from the source type to the destination type.
        /// Supports ignoring specific properties either via attribute or dynamically via parameter.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <typeparam name="TDestination">The destination type.</typeparam>
        /// <param name="ignoredProperties">A set of property names to ignore during mapping.</param>
        /// <returns>A compiled function that maps a source object to a destination object.</returns>
        private static Func<TSource, TDestination> CreateMappingFunction<TSource, TDestination>(HashSet<string> ignoredProperties = null)
            where TDestination : new()
        {
            var sourceParameter = Expression.Parameter(typeof(TSource), "source");
            var destinationVariable = Expression.New(typeof(TDestination));
            var memberBindings = new List<MemberBinding>();

            var sourceProperties = typeof(TSource).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var destinationProperties = typeof(TDestination).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var destinationProperty in destinationProperties)
            {
                // Check if property should always be ignored by using SimpleMapperIgnoreAttribute
                var ignoreAttribute = destinationProperty.GetCustomAttribute<SimpleMapperIgnoreAttribute>();
                if (ignoreAttribute != null)
                {
                    continue;
                }

                if (ignoredProperties != null && ignoredProperties.Contains(destinationProperty.Name))
                {
                    continue;
                }

                var customMappingAttribute = destinationProperty.GetCustomAttribute<SimpleMapperMappingAttribute>();
                string sourcePropertyName = customMappingAttribute?.SourcePropertyName ?? destinationProperty.Name;

                var sourceProperty = sourceProperties.FirstOrDefault(sp => sp.Name == sourcePropertyName);

                if (sourceProperty != null && destinationProperty.CanWrite)
                {
                    try
                    {
                        if (destinationProperty.PropertyType == sourceProperty.PropertyType)
                        {
                            var sourceValue = Expression.Property(sourceParameter, sourceProperty);
                            var binding = Expression.Bind(destinationProperty, sourceValue);
                            memberBindings.Add(binding);
                        }
                        else if (destinationProperty.PropertyType.IsEnum && sourceProperty.PropertyType == typeof(string))
                        {
                            var sourceValue = Expression.Property(sourceParameter, sourceProperty);
                            var parseMethod = typeof(Enum).GetMethod("Parse", new[] { typeof(Type), typeof(string), typeof(bool) });
                            var enumParseCall = Expression.Call(parseMethod, Expression.Constant(destinationProperty.PropertyType), sourceValue, Expression.Constant(true));
                            var binding = Expression.Bind(destinationProperty, Expression.Convert(enumParseCall, destinationProperty.PropertyType));
                            memberBindings.Add(binding);
                        }
                        else if (destinationProperty.PropertyType == typeof(string) && sourceProperty.PropertyType.IsEnum)
                        {
                            var sourceValue = Expression.Property(sourceParameter, sourceProperty);
                            var toStringCall = Expression.Call(sourceValue, "ToString", null);
                            var binding = Expression.Bind(destinationProperty, toStringCall);
                            memberBindings.Add(binding);
                        }
                        else
                        {
                            // More complex type conversion handling
                            var sourceValue = Expression.Property(sourceParameter, sourceProperty);
                            var convertMethod = typeof(Convert).GetMethod("ChangeType", new[] { typeof(object), typeof(Type) });
                            var convertedValue = Expression.Call(convertMethod, Expression.Convert(sourceValue, typeof(object)), Expression.Constant(destinationProperty.PropertyType));
                            var binding = Expression.Bind(destinationProperty, Expression.Convert(convertedValue, destinationProperty.PropertyType));
                            memberBindings.Add(binding);
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"Error mapping property {sourceProperty.Name} to {destinationProperty.Name}: {ex.Message}", ex);
                    }
                }
            }

            var initializer = Expression.MemberInit(destinationVariable, memberBindings);
            var lambda = Expression.Lambda<Func<TSource, TDestination>>(initializer, sourceParameter);
            return lambda.Compile();
        }
    }
}
