using SimpleMapperUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTestXUnitNet6
{
    public class DestinationWithIgnoredProperties
    {
        public string Name { get; set; }

        [SimpleMapperIgnore]
        public string IgnoredProperty { get; set; }
    }
}
