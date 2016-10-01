using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EasyMySql.Core
{
    public enum FilterType { And, Or }

    public sealed class Filter
    {
        public FilterType filterType { get; private set; }
        public string[] PropertyNames { get; private set; }
        public string[] valueNames { get; private set; }
        public object[] values { get; private set; }

        public Filter(FilterType filterType, string[] PropertyNames, string[] valueNames, object[] values)
        {
            this.filterType = filterType;

            if (PropertyNames.Length == values.Length && valueNames.Length == values.Length)
            {
                this.PropertyNames = PropertyNames;
                this.valueNames = valueNames;
                this.values = values;
            }
        }

        public override string ToString()
        {
            string Output = " ";

            if (PropertyNames != null && values != null)
            {
                for (int i = 0; i < PropertyNames.Length; i++)
                {
                    Output += PropertyNames[i] + " = @" + valueNames[i] + " " + filterType.ToString() + " ";
                }

                Output = Output.Substring(0, Output.Length - (filterType.ToString().Length + 1));
            }

            return Output;
        }
    }
}
