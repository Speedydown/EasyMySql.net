using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyMySql.Core
{
    public sealed class Field
    {
        public string FieldName { get; private set; }
        public Type FieldType { get; private set; }
        public int Size { get; private set; }
        public bool Key { get; private set; }

        public Field(string FieldName, Type FieldType, int Size, bool Key = false)
        {
            this.FieldName = FieldName;
            this.FieldType = FieldType;
            this.Size = Size;
            this.Key = Key;
        }

    }
}
