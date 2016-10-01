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
            tableName = Constants.InternalTablePrefix + "EasyMySqlException";
            LogErrors = false;
            LogDatabaseStats = false;
        }

        public EasyMySqlException EasyMySqlExceptionByID(int ID)
        {
            return GetObjectByID(ID);
        }

        public EasyMySqlException[] getExcptionsByDataHandlerName(string DataHandlerName)
        {
            return GetObjectsBySqlQuery("SELECT * FROM " + tableName + " WHERE DatahandlerName = @DataHandlerName ORDER BY ID DESC",
                new string[] { "@DataHandlerName"},
                new object[] { DataHandlerName });
        }

        public EasyMySqlException[] GetWebsiteExceptionList()
        {
            return GetObjectList(0, OrderBy.DESC, "ID");
        }

        public EasyMySqlException[] GetWebsiteExcptionByText(string Text, bool Exact)
        {
            return GetObjectByPropertyValueAndSearchQuery("TheException", Text, Exact, 25, OrderBy.ASC, "ID");
        }

        public EasyMySqlException[] GetExceptionByHash(string Hash)
        {
            return GetObjectsBySqlQuery("SELECT * FROM " + tableName + " WHERE ExceptionHash = @ExceptionHash",
                new string[] { "@ExceptionHash" },
                new object[] { Hash });
        }

        public EasyMySqlException UpdateWebsiteException(EasyMySqlException Exception)
        {
            if (Exception.ID == 0)
            {
                return AddObject(Exception);
            }

            Exception.TimeStamp = DateTime.Now;
            return UpdateObject(Exception);
        }

        public bool DeleteWebsiteException(int ID)
        {
            return DeleteObject(ID);
        }

        public int GetExceptionsCount()
        {
            return GetObjectCountWithSqlQuery("SELECT COUNT(*) AS Count FROM " + tableName
                , new string[] { },
                new object[] { });
        }
    }
}