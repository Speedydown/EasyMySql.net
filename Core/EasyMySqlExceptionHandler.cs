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

        private static Field DatahandlerNameField = new Field("DatahandlerName", typeof(string), 175);
        private static Field TheExceptionField = new Field("TheException", typeof(string), 3000);
        private static Field TimeStampField = new Field("TimeStamp", typeof(DateTime), 45);
        private static Field ExceptionHashField = new Field("ExceptionHash", typeof(string), 250);

        private EasyMySqlExceptionHandler() : base(Constants.InternalTablePrefix + "EasyMySqlException", new Field[] {
                DatahandlerNameField,
                TheExceptionField,
                TimeStampField,
                ExceptionHashField
                    })
        {
            RegisterError = false;
            LogDatabaseStats = false;
        }

        public EasyMySqlException EasyMySqlExceptionByID(int ID)
        {
            return base.GetObjectByID(ID);
        }

        public EasyMySqlException[] getExcptionsByDataHandlerName(string DataHandlerName)
        {
            return GetObjectsBySqlQuery("SELECT * FROM " + tableName + " WHERE " + DatahandlerNameField.FieldName + " = @DataHandlerName ORDER BY ID DESC",
                new string[] { "@DataHandlerName"},
                new object[] { DataHandlerName });
        }

        public EasyMySqlException[] GetWebsiteExceptionList()
        {
            return GetObjectList(0, OrderBy.DESC, IDField);
        }

        public EasyMySqlException[] GetWebsiteExcptionByText(string Text, bool Exact)
        {
            return GetObjectByFieldsAndSearchQuery(TheExceptionField, Text, Exact, 25, OrderBy.ASC, new Field("ID", typeof(int), 1));
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