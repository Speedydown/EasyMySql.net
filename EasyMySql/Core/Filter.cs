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
        public List<string> PropertyNames { get; private set; }
        public List<object> values { get; private set; }

        public Filter(FilterType filterType, IEnumerable<string> PropertyNames, IEnumerable<object> values)
        {
            this.filterType = filterType;

            if (PropertyNames?.Count() == values?.Count())
            {
                this.PropertyNames = PropertyNames?.ToList() ?? new List<string>();
                this.values = values?.ToList() ?? new List<object>();
            }
        }

        /// <summary>
        /// Adds a condition to this filter
        /// </summary>
        /// <param name="PropertyName">Example: ID</param>
        /// <param name="ValueName">Example: ID</param>
        /// <param name="Value">Example: 1</param>
        public void AddCondition(string PropertyName, object Value)
        {
            if (!string.IsNullOrWhiteSpace(PropertyName) && values != null)
            {
                PropertyNames.Add(PropertyName);
                values.Add(Value);
            }
        }

        public override string ToString()
        {
            string Output = " ";

            if (PropertyNames != null && values != null)
            {
                for (int i = 0; i < PropertyNames.Count; i++)
                {
                    Output += PropertyNames[i] + " = @" + PropertyNames[i] + i + " " + filterType.ToString() + " ";
                }

                Output = Output.Substring(0, Output.Length - (filterType.ToString().Length + 1));
            }

            return Output;
        }
    }
}
