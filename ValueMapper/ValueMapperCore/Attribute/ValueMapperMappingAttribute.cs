namespace ValueMapperUtility.Attribute
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ValueMapperMappingAttribute : System.Attribute
    {
        public string SourcePropertyName { get; }

        public ValueMapperMappingAttribute(string sourcePropertyName)
        {
            SourcePropertyName = sourcePropertyName;
        }
    }
} 