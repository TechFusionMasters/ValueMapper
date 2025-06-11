using SimpleMapperUtility;
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
    }
    public class NumericDestination
    {
        [SimpleMapperMapping("IntValue")]
        public long LongValue { get; set; }
        [SimpleMapperMapping("DoubleValue")]
        public float FloatValue { get; set; }
        [SimpleMapperMapping("StringNumber")]
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

    // Custom class with SimpleMapperIgnoreAttribute
    public class SourceWithIgnore
    {
        public string Name { get; set; }

        [SimpleMapperIgnore]
        public string IgnoredProperty { get; set; }
    }

    public class DestinationWithIgnore
    {
        public string Name { get; set; }
        public string IgnoredProperty { get; set; }
    }
}
