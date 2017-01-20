using EasyMySql.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;

namespace EasyMySql.Core
{
    public sealed class EasyMySqlException : DataObject
    {
        [LengthAttribute(Length = 125)]
        public string DatahandlerName { get; private set; }
        [LengthAttribute(Length = 250)]
        public string TheException { get; private set; }
        public DateTime TimeStamp { get; internal set; }
        [LengthAttribute(Length = 350)]
        public string ExceptionHash { get; private set; }

        public EasyMySqlException()
        {

        }

        public EasyMySqlException(object Datahandler, Exception e) : this(0, Datahandler.ToString(), e.Message, TimeConverter.GetDateTime(), Encrypt.HashString(e.ToString()))
        {
            EasyMySqlLog.Log(Datahandler, "Exception occurred:\n" + e.ToString(), logSeverity.Error);
        }

        public EasyMySqlException(int ID, string DatahandlerName, string TheException, DateTime TimeStamp, string ExceptionHash)
        {
            this.ID = ID;
            this.DatahandlerName = DatahandlerName;
            this.TheException = TheException.Replace('<', '[').Replace('>',']');
            this.TimeStamp = TimeStamp;
            this.ExceptionHash = ExceptionHash;

            if (this.ID == 0 && !this.TheException.StartsWith("System.Threading.ThreadAbortException"))
            {
                EasyMySqlException[] wList = EasyMySqlExceptionHandler.instance.GetExceptionByHash(ExceptionHash);

                if (wList.Count() == 0)
                {
                    EasyMySqlExceptionHandler.instance.Add(this);
                }
                else
                {
                    EasyMySqlExceptionHandler.instance.DeleteWebsiteException(wList.First().ID);
                    EasyMySqlExceptionHandler.instance.Add(this);
                }
            }
        }
    }
}