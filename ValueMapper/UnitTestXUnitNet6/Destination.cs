using ValueMapperUtility;
using ValueMapperUtility.Attribute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static UnitTestXUnitNet6.ValueMapperTests;

namespace UnitTestXUnitNet6
{
    public class Destination
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public string City { get; set; }
        public SampleEnum EnumValue { get; set; }
        public string CustomName { get; set; }
    }

}
