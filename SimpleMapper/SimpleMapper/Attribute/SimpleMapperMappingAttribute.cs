using System;

namespace SimpleMapperUtility
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class SimpleMapperMappingAttribute : Attribute
    {
        /// <summary>
        /// Specifies the source property name that should be mapped to the target property.
        /// </summary>
        public string SourcePropertyName { get; }

        public SimpleMapperMappingAttribute(string sourcePropertyName)
        {
            if (string.IsNullOrWhiteSpace(sourcePropertyName))
                throw new ArgumentException("Source property name cannot be null or whitespace", nameof(sourcePropertyName));

            SourcePropertyName = sourcePropertyName;
        }
    }
}
