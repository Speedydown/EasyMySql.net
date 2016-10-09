using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EasyMySql.Core
{
    internal class DataObjectPropertyInfo
    {
        public string Name { get; set; }
        public Type Type { get; set; }
        public int Length { get; set; }
        public bool IsPrimaryKey { get; set; }
    }
}
