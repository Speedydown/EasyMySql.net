using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EasyMySql.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class StringLengthAttribute : Attribute
    {
        private int _VarcharLength;
        public int VarcharLength
        {
            get
            {
                return _VarcharLength;
            }
            set
            {

                _VarcharLength = value;
            }
        }

        public StringLengthAttribute()
        {
            VarcharLength = 250;
        }
    }
}
