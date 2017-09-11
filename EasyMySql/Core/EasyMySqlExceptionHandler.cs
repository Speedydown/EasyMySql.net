using EasyMySql.Utils;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EasyMySql.Core
{
    public sealed class EasyMySqlExceptionHandler : DataHandler<EasyMySqlException>
    {
        public static readonly EasyMySqlExceptionHandler instance = new EasyMySqlExceptionHandler();

        private EasyMySqlExceptionHandler()
        {
            TableName = Constants.InternalTablePrefix + "EasyMySqlException";
            LogErrors = false;
            LogDatabaseStats = false;
        }

        public EasyMySqlException EasyMySqlExceptionByID(int ID)
        {
            return GetItem(ID);
        }

        public EasyMySqlException[] getExcptionsByDataHandlerName(string DataHandlerName)
        {
            return GetItems("SELECT * FROM " + TableName + " WHERE DatahandlerName = @DataHandlerName ORDER BY ID DESC",
                new string[] { "@DataHandlerName"},
                new object[] { DataHandlerName });
        }

        public EasyMySqlException[] GetWebsiteExceptionList()
        {
            return GetItems(0, OrderBy.DESC, "ID");
        }

        public EasyMySqlException[] GetWebsiteExcptionByText(string Text, bool Exact)
        {
            return GetItems("TheException", Text, Exact, 25, OrderBy.ASC, "ID");
        }

        public EasyMySqlException[] GetExceptionByHash(string Hash)
        {
            return GetItems("SELECT * FROM " + TableName + " WHERE ExceptionHash = @ExceptionHash",
                new string[] { "@ExceptionHash" },
                new object[] { Hash });
        }

        public EasyMySqlException UpdateWebsiteException(EasyMySqlException Exception)
        {
            if (Exception.ID == 0)
            {
                return Add(Exception);
            }

            Exception.TimeStamp = DateTime.Now;
            return Update(Exception);
        }

        public bool DeleteWebsiteException(int ID)
        {
            return Delete(ID);
        }

        public int GetExceptionsCount()
        {
            return CountItems("SELECT COUNT(*) AS Count FROM " + TableName
                , new string[] { },
                new object[] { });
        }
    }
}