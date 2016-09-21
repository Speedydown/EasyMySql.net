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
        public Field[] fields { get; private set; }
        public string[] valueNames { get; private set; }
        public object[] values { get; private set; }

        public Filter(FilterType filterType, Field[] fields, string[] valueNames, object[] values)
        {
            this.filterType = filterType;

            if (fields.Length == values.Length && valueNames.Length == values.Length)
            {
                this.fields = fields;
                this.valueNames = valueNames;
                this.values = values;
            }
        }

        public override string ToString()
        {
            string Output = " ";

            if (fields != null && values != null)
            {
                for (int i = 0; i < fields.Length; i++)
                {
                    Output += fields[i].FieldName + " = @" + valueNames[i] + " " + filterType.ToString() + " ";
                }

                Output = Output.Substring(0, Output.Length - (filterType.ToString().Length + 1));
            }

            return Output;
        }
    }
}
