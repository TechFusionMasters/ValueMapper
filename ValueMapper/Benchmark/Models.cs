using ValueMapperUtility;
using ValueMapperUtility.Attribute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benchmark
{
    // Simple classes with just a few properties
    public class SourceSimple
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class DestSimple
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    // More complex classes with nested objects
    public class SourceComplex
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public SourceSimple Child { get; set; }
        public List<string> Tags { get; set; }
        public Dictionary<string, int> Counts { get; set; }
        public DateTime CreatedDate { get; set; }
        public TimeSpan Duration { get; set; }
        public decimal Price { get; set; }
        public bool IsActive { get; set; }
    }

    public class DestComplex
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DestSimple Child { get; set; }
        public List<string> Tags { get; set; }
        public Dictionary<string, int> Counts { get; set; }
        public DateTime CreatedDate { get; set; }
        public TimeSpan Duration { get; set; }
        public decimal Price { get; set; }
        public bool IsActive { get; set; }
    }

    // Classes for testing type conversions
    public enum TestEnum
    {
        Value1 = 1,
        Value2 = 2,
        Value3 = 3
    }

    public class SourceWithDifferentTypes
    {
        public int IntProperty { get; set; }
        public double DoubleProperty { get; set; }
        public decimal DecimalProperty { get; set; }
        public string StringProperty { get; set; }
        public DateTime DateTimeProperty { get; set; }
        public TestEnum EnumProperty { get; set; }
    }

    public class DestWithDifferentTypes
    {
        public long IntProperty { get; set; }  // int to long
        public int DoubleProperty { get; set; } // double to int
        public float DecimalProperty { get; set; } // decimal to float
        public int StringProperty { get; set; } // string to int
        public string DateTimeProperty { get; set; } // datetime to string
        public string EnumProperty { get; set; } // enum to string
    }

    // Classes for testing custom property mapping
    public class SourceWithCustomNames
    {
        [ValueMapperMapping("DestinationCustomName")]
        public string SourceProperty { get; set; }
        public int AnotherProperty { get; set; }
    }

    public class DestWithCustomNames
    {
        [ValueMapperMapping("SourceProperty")]
        public string DestinationCustomName { get; set; }
        public int AnotherProperty { get; set; }
    }
}
