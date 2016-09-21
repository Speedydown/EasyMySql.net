using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace EasyMySql.Core
{
    public abstract class DataObject
    {
        public int ID { get; protected set; }

        public DataObject(int ID)
        {
            this.ID = ID;
        }

        internal void setID(int ID)
        {
            this.ID = ID;
        }

        internal void TrimValues()
        {
            foreach (PropertyInfo propertyInfo in this.GetType().GetProperties())
            {
                try
                {
                    if (propertyInfo.PropertyType == typeof(string))
                    {
                        
                        string PropValue = propertyInfo.GetValue(this, null) as string;
                        propertyInfo.SetValue(this, HttpUtility.HtmlDecode(PropValue.Trim()), null);
                    }
                }
                catch (Exception)
                {

                }
            }
        }

        public override string ToString()
        {
            return this.GetType().Name;
        }
    }
}
