using EasyMySql.Attributes;
using EasyMySql.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyMySql_Test.Model
{
    public class Test : DataObject
    {
        public int IntValue { get; set; }
        public double DoubleValue { get; set; }
        public bool BoolValue { get; set; }
        [StringLength(Length = 100)]
        public string StringValue { get; set; }
        public DateTime DateTimeValue { get; set; }
        [Ignore]
        public bool IgnoredValue { get; set; }
        [StringLength(Length = 250)]
        public string LongerString { get; set; }
    }
}
