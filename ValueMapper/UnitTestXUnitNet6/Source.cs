using ValueMapperUtility;
using ValueMapperUtility.Attribute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTestXUnitNet6
{
    public class Source
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public string City { get; set; }
        public string EnumValue { get; set; }
        [ValueMapperMapping("CustomName")]
        public string CustomMappedProperty { get; set; }
    }
}
