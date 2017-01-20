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
        [Unique]
        public double DoubleValue { get; set; }
        public bool BoolValue { get; set; }
        [Length(Length = 100)][Unique]
        public string StringValue { get; set; }
        public DateTime DateTimeValue { get; set; }
        [Ignore]
        public bool IgnoredValue { get; set; }
        [Length(Length = 250)]
        public string LongerString { get; set; }
    }
}
