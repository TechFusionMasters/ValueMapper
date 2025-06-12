using ValueMapperUtility;
using ValueMapperUtility.Attribute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTestXUnitNet6
{

    public enum SampleEnum
    {
        None,
        Value1,
        Value2
    }
    public class Models
    {
        
        public int IntValue { get; set; }
        
        public double DoubleValue { get; set; }
        public string StringNumber { get; set; }
        public string StringValue { get; set; }
    }
    public class NumericDestination
    {
        [ValueMapperMapping("IntValue")]
        public long LongValue { get; set; }

        [ValueMapperMapping("DoubleValue")]
        public float FloatValue { get; set; }

        [ValueMapperMapping("StringNumber")]
        public int IntFromString { get; set; }
    }

    public class BasicSource
    {
        public string Value { get; set; }
    }

    public class NullableDestination
    {
        public int? NullableValue { get; set; }
    }

    public class NonNullableDestination
    {
        public int IntValue { get; set; }
    }

    // Custom class with ValueMapperIgnoreAttribute
    public class SourceWithIgnoredProperties
    {
        public string Name { get; set; }

        [ValueMapperIgnore]
        public string IgnoredProperty { get; set; }
    }

    public class DestinationWithIgnore
    {
        public string Name { get; set; }
        public string IgnoredProperty { get; set; }
    }
}
