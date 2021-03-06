﻿using EasyMySql.Attributes;
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
        [Key]
        public virtual int ID { get; set; }

        public DataObject()
        {
            
        }

        internal void TrimValues()
        {
            foreach (PropertyInfo propertyInfo in GetType().GetProperties())
            {
                try
                {
                    if (propertyInfo.PropertyType == typeof(string))
                    {
                        
                        string PropValue = propertyInfo.GetValue(this, null) as string;

                        if (PropValue == null)
                        {
                            PropValue = string.Empty;
                        }

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
            return GetType().Name;
        }
    }
}
