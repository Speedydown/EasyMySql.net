using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EasyMySql.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class LengthAttribute : Attribute
    {
        private int _Length;
        public int Length
        {
            get
            {
                return _Length;
            }
            set
            {

                _Length = value;
            }
        }

        public LengthAttribute()
        {
            Length = 250;
        }
    }
}
