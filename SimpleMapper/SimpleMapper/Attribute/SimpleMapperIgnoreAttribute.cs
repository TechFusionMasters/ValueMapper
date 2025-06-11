using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleMapperUtility
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class SimpleMapperIgnoreAttribute : Attribute
    {
        /// <summary>
        /// Marks a property to be ignored during mapping.
        /// </summary>
    }
}
