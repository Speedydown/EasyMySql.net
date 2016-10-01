using EasyMySql.Attributes;
using EasyMySql.Performance;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EasyMySql.Core
{
    public enum OrderBy { ASC, DESC }

    public class DataHandler<T> where T : DataObject, new()
    {
        protected bool LogErrors { get; set; }
        protected bool LogDatabaseStats { get; set; }
        protected T DefaultDataObject { get; set; }
        public string tableName { get; protected set; }
        private DataObjectPropertyInfo[] PropertyInfo { get; set; }
        protected Type objectType { get; set; }

        public DataHandler()
        {
            objectType = typeof(T);
            tableName = objectType.Name;
            PropertyInfo = GetPropertyInfo();
        }

        private T[] GetDataObjects(MySqlDataReader Reader)
        {
            List<T> CurrentObjects = new List<T>();

            if (Reader.HasRows)
            {
                try
                {
                    while (Reader.Read())
                    {
                        T CurrentObject = new T();

                        foreach (DataObjectPropertyInfo pi in PropertyInfo)
                        {
                            if (pi.Type == typeof(bool))
                            {
                                objectType.GetProperty(pi.Name).SetValue(CurrentObject, Convert.ToBoolean(Reader[pi.Name]), null);
                            }
                            else if (pi.Type == typeof(DateTime))
                            {
                                objectType.GetProperty(pi.Name).SetValue(CurrentObject, new DateTime((long)Reader[pi.Name]), null);
                            }
                            else
                            {
                                objectType.GetProperty(pi.Name).SetValue(CurrentObject, Reader[pi.Name], null);
                            }

                        }

                        try
                        {
                            CurrentObjects.Add(CurrentObject);
                        }
                        catch (TargetInvocationException)
                        {
                            EasyMySqlLog.Log(this, "Could not get dataobjects from table: " + tableName, logSeverity.Error);

                        }
                    }
                }
                catch (InvalidCastException)
                {
                    //Row is empty
                }
                catch (Exception e)
                {
                    DatabaseHandler.CloseConnectionByReader(Reader);
                    throw e;
                }
            }

            DatabaseHandler.CloseConnectionByReader(Reader);

            return CurrentObjects.ToArray();
        }

        private DataObjectPropertyInfo[] GetPropertyInfo()
        {
            List<DataObjectPropertyInfo> PropertyInfoList = new List<DataObjectPropertyInfo>();

            try
            {
                foreach (PropertyInfo pi in objectType.GetProperties())
                {
                    bool IsPrimaryKey = false;
                    bool HasIgnoreAttribute = false;
                    int StringLength = 250;

                    foreach (object attribute in pi.GetCustomAttributes(true))
                    {
                        if (attribute is IgnorePropertyAttribute)
                        {
                            HasIgnoreAttribute = true;
                            break;
                        }

                        if (attribute is PrimaryKeyAttribute)
                        {
                            IsPrimaryKey = true;
                        }

                        if (pi.PropertyType == typeof(string) && attribute is StringLengthAttribute)
                        {
                            StringLength = (attribute as StringLengthAttribute).VarcharLength;
                        }
                    }

                    if (HasIgnoreAttribute)
                    {
                        continue;
                    }

                    PropertyInfoList.Add(new DataObjectPropertyInfo()
                    {
                        Name = pi.Name,
                        Type = pi.PropertyType,
                        Length = pi.PropertyType.Equals(typeof(string)) ? StringLength : 1,
                        IsPrimaryKey = IsPrimaryKey,
                    });
                }
            }
            catch (Exception e)
            {
                EasyMySqlLog.Log(this, e.Message, logSeverity.Critical);
            }

            return PropertyInfoList.ToArray();
        }

        private bool CreateTable()
        {
            try
            {
                DataObjectPropertyInfo PrimaryKey = null;

                MySqlCommand Command = new MySqlCommand();
                Command.CommandText = "CREATE TABLE IF NOT EXISTS " + tableName + " ( ";

                foreach (DataObjectPropertyInfo pi in PropertyInfo)
                {
                    if (pi.Type == typeof(int))
                    {
                        Command.CommandText += " " + pi.Name + " INT NOT NULL " + (pi.IsPrimaryKey ? " AUTO_INCREMENT" : "") + " ,";

                        if (pi.IsPrimaryKey)
                        {
                            if (PrimaryKey == null)
                            {
                                PrimaryKey = pi;
                            }
                            else
                            {
                                throw new Exception("Instance has 2 primary keys");
                            }
                        }
                    }
                    else if (pi.Type == typeof(string))
                    {
                        Command.CommandText += " " + pi.Name + " VARCHAR(" + pi.Length + ") NOT NULL ,";
                    }
                    else if (pi.Type == typeof(bool))
                    {
                        Command.CommandText += " " + pi.Name + " BIT NOT NULL ,";
                    }
                    else if (pi.Type == typeof(DateTime))
                    {
                        Command.CommandText += " " + pi.Name + " BIGINT NOT NULL ,";
                    }
                    else if (pi.Type == typeof(double))
                    {
                        Command.CommandText += " " + pi.Name + " FLOAT NOT NULL ,";
                    }
                }

                if (PrimaryKey != null)
                {
                    Command.CommandText += "PRIMARY KEY (" + PrimaryKey.Name + "), " +
                        "UNIQUE KEY ID_UNIQUE (" + PrimaryKey.Name + ") );";
                }
                else
                {
                    Command.CommandText = Command.CommandText.Substring(0, Command.CommandText.Length - 2);
                    Command.CommandText += " );";
                }

                DatabaseHandler.ExecuteNonQuery(Command, LogDatabaseStats);

                if (DefaultDataObject != null)
                {
                    AddObject(DefaultDataObject);
                }

                return true;
            }
            catch (Exception e)
            {
                if (e.Message.Contains("Multiple primary key defined"))
                {
                    return false;
                }

                exceptionHandler(e, "CreateTable");
                return false;
            }
        }

        private bool RestructureTable()
        {
            DataObjectPropertyInfo PrimaryKey = null;

            foreach (DataObjectPropertyInfo pi in PropertyInfo)
            {
                try
                {
                    MySqlCommand Command = new MySqlCommand();
                    if (pi.Type == typeof(int))
                    {
                        Command.CommandText = "ALTER TABLE " + tableName + " ADD COLUMN " + pi.Name + " INT NOT NULL " + (pi.IsPrimaryKey ? " AUTO_INCREMENT" : "");

                        if (pi.IsPrimaryKey)
                        {
                            if (PrimaryKey == null)
                            {
                                PrimaryKey = pi;
                            }
                            else
                            {
                                throw new Exception("Instance has 2 primary keys");
                            }
                        }
                    }

                    if (pi.Type == typeof(string))
                    {
                        Command.CommandText = "ALTER TABLE " + tableName + " ADD COLUMN " + pi.Name + " VARCHAR(" + pi.Length + ") NOT NULL ";
                    }

                    if (pi.Type == typeof(bool))
                    {
                        Command.CommandText = "ALTER TABLE " + tableName + " ADD COLUMN " + pi.Name + " BIT NOT NULL ";
                    }

                    if (pi.Type == typeof(DateTime))
                    {
                        Command.CommandText = "ALTER TABLE " + tableName + " ADD COLUMN " + pi.Name + " BIGINT NOT NULL ";
                    }

                    if (pi.Type == typeof(double))
                    {
                        Command.CommandText = "ALTER TABLE " + tableName + " ADD COLUMN " + pi.Name + " FLOAT NOT NULL ";
                    }

                    DatabaseHandler.ExecuteNonQuery(Command, this.LogDatabaseStats);
                }
                catch (Exception e)
                {
                    if (e.Message.Contains("Duplicate column name "))
                    {
                        try
                        {
                            MySqlCommand Command = new MySqlCommand();
                            if (pi.Type == typeof(int))
                            {
                                Command.CommandText = "ALTER TABLE " + tableName + " CHANGE COLUMN " + pi.Name + " " + pi.Name + " INT NOT NULL " + (pi.IsPrimaryKey ? " AUTO_INCREMENT" : "");

                                if (pi.IsPrimaryKey && PrimaryKey == null)
                                {
                                    PrimaryKey = pi;
                                }
                                else
                                {
                                    if (PrimaryKey != pi && pi.IsPrimaryKey)
                                    {
                                        throw new Exception("Instance has 2 primary keys");
                                    }
                                }

                            }

                            if (pi.Type == typeof(string))
                            {
                                Command.CommandText = "ALTER TABLE " + tableName + " CHANGE COLUMN " + pi.Name + " " + pi.Name + " VARCHAR(" + pi.Length + ") NOT NULL ";
                            }

                            if (pi.Type == typeof(bool))
                            {
                                Command.CommandText = "ALTER TABLE " + tableName + " CHANGE COLUMN " + pi.Name + " " + pi.Name + " BIT NOT NULL ";
                            }

                            if (pi.Type == typeof(DateTime))
                            {
                                Command.CommandText = "ALTER TABLE " + tableName + " CHANGE COLUMN " + pi.Name + " " + pi.Name + " BIGINT NOT NULL ";
                            }

                            if (pi.Type == typeof(double))
                            {
                                Command.CommandText = "ALTER TABLE " + tableName + " CHANGE COLUMN " + pi.Name + " " + pi.Name + " FLOAT NOT NULL ";
                            }

                            DatabaseHandler.ExecuteNonQuery(Command, LogDatabaseStats);
                        }
                        catch (Exception)
                        {
                            return false;
                        }
                    }
                    else if (e.Message.Contains("Multiple primary key defined"))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            if (PrimaryKey != null)
            {
                try
                {
                    MySqlCommand Command = new MySqlCommand();
                    Command.CommandText = "alter TABLE " + tableName + " ADD PRIMARY KEY (`" + PrimaryKey.Name + "`);";
                    DatabaseHandler.ExecuteNonQuery(Command, LogDatabaseStats);
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("Multiple"))
                    {
                        return true;
                    }

                    if (LogErrors)
                    {
                        new EasyMySqlException(this, ex);
                    }

                    return false;
                }
            }

            return true;
        }

        public virtual T AddObject(T DataObject)
        {
            DataObject.TrimValues();

            try
            {
                MySqlCommand Command = new MySqlCommand();
                Command.CommandText = "INSERT INTO " + tableName + " ( ";

                foreach (DataObjectPropertyInfo pi in PropertyInfo)
                {
                    if (pi.Name != "ID")
                    {
                        Command.CommandText += pi.Name + ", ";
                    }
                }

                Command.CommandText = Command.CommandText.Substring(0, Command.CommandText.Length - 2);
                Command.CommandText += ") VALUES ( ";

                foreach (DataObjectPropertyInfo pi in PropertyInfo)
                {
                    if (pi.Name != "ID")
                    {
                        Command.CommandText += " @" + pi.Name + ", ";
                    }
                }

                Command.CommandText = Command.CommandText.Substring(0, Command.CommandText.Length - 2);
                Command.CommandText += " )";

                for (int i = 0; i < PropertyInfo.Count(); i++)
                {
                    if (PropertyInfo[i].Name != "ID")
                    {
                        if (PropertyInfo[i].Type != typeof(DateTime))
                        {
                            object FieldValue = DataObject.GetType().GetProperty(PropertyInfo[i].Name).GetValue(DataObject, null);

                            Command.Parameters.AddWithValue("@" + PropertyInfo[i].Name, FieldValue);

                            if (PropertyInfo[i].Type == typeof(string) && FieldValue.ToString().Length > PropertyInfo[i].Length)
                            {
                                EasyMySqlLog.Log(this, "Field " + PropertyInfo[i].Name + " has exceeded its size, Consider increasing its fieldsize. Do not forgot to force a restruct later.", logSeverity.Warning);
                            }
                        }
                        else
                        {
                            DateTime dt = (DateTime)DataObject.GetType().GetProperty(PropertyInfo[i].Name).GetValue(DataObject, null);
                            Command.Parameters.AddWithValue("@" + PropertyInfo[i].Name, dt.Ticks);
                        }
                    }
                }

                DatabaseHandler.ExecuteNonQuery(Command, LogDatabaseStats);

                if (LogErrors)
                {
                    EasyMySqlLog.Log(this, DataObject.ToString() + " with id " + Command.LastInsertedId + " has been added.", logSeverity.Info);
                }

                DataObject.ID = (Convert.ToInt32(Command.LastInsertedId));
                CacheHandler.ClearData(ToString());
                return DataObject;
            }
            catch (Exception e)
            {
                if (exceptionHandler(e, "AddObject"))
                {
                    return AddObject(DataObject);
                }
                else
                {
                    return null;
                }

            }
        }

        public virtual T UpdateObject(T dataObject)
        {
            dataObject.TrimValues();

            IList<PropertyInfo> props = new List<PropertyInfo>(objectType.GetProperties());

            if (dataObject.ID == 0)
            {
                throw new Exception("ID can not be 0. try adding this object first.");
            }

            try
            {
                MySqlCommand Command = new MySqlCommand();
                Command.CommandText = "UPDATE " + tableName + " SET ";

                foreach (DataObjectPropertyInfo pi in PropertyInfo)
                {
                    if (pi.Name != "ID")
                    {
                        Command.CommandText += pi.Name + " = @" + pi.Name + ", ";

                        foreach (PropertyInfo opi in props)
                        {
                            if (opi.Name == pi.Name)
                            {
                                if (opi.PropertyType == typeof(DateTime))
                                {
                                    DateTime dt = (DateTime)opi.GetValue(dataObject, null);

                                    Command.Parameters.AddWithValue("@" + pi.Name, dt.Ticks);
                                    break;
                                }
                                else
                                {
                                    Command.Parameters.AddWithValue("@" + pi.Name, opi.GetValue(dataObject, null));
                                    break;
                                }
                            }
                        }
                    }
                }

                Command.CommandText = Command.CommandText.Substring(0, Command.CommandText.Length - 2);

                Command.CommandText += " WHERE ID = @ID";
                Command.Parameters.AddWithValue("ID", dataObject.ID);

                DatabaseHandler.ExecuteNonQuery(Command, LogDatabaseStats);

                if (LogErrors)
                {
                    EasyMySqlLog.Log(this, dataObject.ToString() + " with id " + dataObject.ID + " has been updated.", logSeverity.Info);
                }

                CacheHandler.ClearData(ToString());

                return dataObject;
            }
            catch (Exception e)
            {
                if (exceptionHandler(e, "UpdateObject"))
                {
                    return UpdateObject(dataObject);
                }
                else
                {
                    return null;
                }
            }
        }

        public virtual T GetObjectByID(int ID)
        {
            try
            {
                T dataObject = (T)CacheHandler.GetData(ToString(), "GetObjectByID", new string[] { Convert.ToString(ID) });

                if (dataObject != null)
                {
                    return dataObject;
                }

                MySqlCommand Command = new MySqlCommand();
                Command.CommandText = "SELECT * FROM " + tableName + " WHERE ID = @ID";
                Command.Parameters.AddWithValue("@ID", ID);

                T[] ObjectList = GetDataObjects(DatabaseHandler.ExecuteQuery(Command, LogDatabaseStats));
                QueryTrace.RemoveQuery(Command.CommandText);

                if (ObjectList.Count() > 0)
                {
                    CacheHandler.AddToCache(ToString(), "GetObjectByID", new string[] { Convert.ToString(ID) }, ObjectList[0]);
                    return ObjectList[0];
                }
                else
                {
                    return null;
                }
            }
            catch (CouldNotConnectException)
            {
                EasyMySqlLog.Log(this, "Could not connect to the database.", logSeverity.Critical);
                return null;
            }
            catch (Exception e)
            {
                if (exceptionHandler(e, "GetObjectByID=" + ID))
                {
                    return GetObjectByID(ID);
                }
                else
                {
                    return null;
                }
            }
        }

        protected virtual T[] GetObjectByPropertyValueAndSearchQuery(string PropertyName, string SearchQuery, bool Exact = false, int LIMIT = 0, OrderBy orderBy = OrderBy.ASC, string OrderByPropertyName = null)
        {
            return GetObjectByPropertyValueAndSearchQuery(new string[] { PropertyName }, SearchQuery, Exact, LIMIT, orderBy, OrderByPropertyName);
        }

        protected virtual T[] GetObjectByPropertyValueAndSearchQuery(string[] PropertyNames, string SearchQuery, bool Exact = false, int LIMIT = 0, OrderBy orderBy = OrderBy.ASC, string OrderByPropertyName = null)
        {
            List<string> ParameterList = new List<string>();

            foreach (string s in PropertyNames)
            {
                ParameterList.Add(s);
            }

            ParameterList.Add(SearchQuery);

            ParameterList.Add(Exact.ToString());
            ParameterList.Add(LIMIT.ToString());
            ParameterList.Add(orderBy.ToString());

            try
            {
                if (OrderByPropertyName != null)
                {
                    ParameterList.Add(OrderByPropertyName);
                }
            }
            catch (Exception)
            {

            }

            T[] dataObjects = (T[])CacheHandler.GetData(ToString(), "GetObjectByFieldsAndSearchQuery", ParameterList.ToArray());

            if (dataObjects != null)
            {
                return dataObjects;
            }

            MySqlCommand Command = new MySqlCommand();
            string SQLQuery = string.Empty;


            if (!Exact)
            {
                SQLQuery = "%" + SearchQuery + "%";
            }
            else
            {
                SQLQuery = SearchQuery;
            }

            try
            {
                Command.CommandText = "SELECT * FROM " + tableName + " WHERE ";

                foreach (string s in PropertyNames)
                {
                    Command.CommandText += s + " LIKE @QUERY OR ";
                }

                Command.Parameters.AddWithValue("@QUERY", SQLQuery);

                if (PropertyNames.Count() > 0)
                {
                    Command.CommandText = Command.CommandText.Substring(0, Command.CommandText.Length - 3);
                }

                Command.CommandText = addOrderBy(Command.CommandText, orderBy, OrderByPropertyName);

                if (LIMIT > 0)
                {
                    Command.CommandText = addLimit(Command.CommandText, LIMIT);
                }

                T[] dataObjectList = GetDataObjects(DatabaseHandler.ExecuteQuery(Command, LogDatabaseStats));
                QueryTrace.RemoveQuery(Command.CommandText);

                CacheHandler.AddToCache(ToString(), "GetObjectByFieldsAndSearchQuery", ParameterList.ToArray(), dataObjectList);
                return dataObjectList;
            }
            catch (Exception e)
            {
                if (exceptionHandler(e, "GetObjectByFieldsAndSearchQuery=" + SearchQuery))
                {
                    return GetObjectByPropertyValueAndSearchQuery(PropertyNames, SearchQuery, Exact, LIMIT, orderBy, OrderByPropertyName);
                }
                else
                {
                    return new T[] { };
                }
            }
        }

        protected virtual T[] GetObjectsByID(string PropertyName, int ID, int LIMIT, OrderBy orderBy = OrderBy.ASC, string OrderByPropertyName = null)
        {
            List<string> ParameterList = new List<string>();

            ParameterList.Add(PropertyName);
            ParameterList.Add(ID.ToString());
            ParameterList.Add(LIMIT.ToString());
            ParameterList.Add(orderBy.ToString());

            try
            {
                if (OrderByPropertyName != null)
                {
                    ParameterList.Add(OrderByPropertyName);
                }
            }
            catch (Exception)
            {

            }

            T[] dataObjects = (T[])CacheHandler.GetData(ToString(), "GetObjectsByChildID", ParameterList.ToArray());

            if (dataObjects != null)
            {
                return dataObjects;
            }

            MySqlCommand Command = new MySqlCommand();

            try
            {
                Command.CommandText = "SELECT * FROM " + tableName + " WHERE " + PropertyName + " = @" + PropertyName;
                Command.Parameters.AddWithValue("@" + PropertyName, ID);

                Command.CommandText = addOrderBy(Command.CommandText, orderBy, OrderByPropertyName);

                if (LIMIT > 0)
                {
                    Command.CommandText = addLimit(Command.CommandText, LIMIT);
                }

                T[] dataObjectList = GetDataObjects(DatabaseHandler.ExecuteQuery(Command, LogDatabaseStats));
                QueryTrace.RemoveQuery(Command.CommandText);

                CacheHandler.AddToCache(ToString(), "GetObjectsByChildID", ParameterList.ToArray(), dataObjectList);
                return dataObjectList;
            }
            catch (Exception e)
            {
                if (exceptionHandler(e, "GetObjectsByChildID=" + PropertyName + ":" + ID))
                {
                    return GetObjectsByID(PropertyName, ID, LIMIT, orderBy, OrderByPropertyName);
                }
                else
                {
                    return new T[] { };
                }
            }
        }

        public virtual T[] GetObjectList(int LIMIT = 0, OrderBy orderBy = OrderBy.ASC, string OrderByPropertyName = null)
        {
            List<string> ParameterList = new List<string>();

            ParameterList.Add(LIMIT.ToString());
            ParameterList.Add(orderBy.ToString());

            try
            {
                if (OrderByPropertyName != null)
                {
                    ParameterList.Add(OrderByPropertyName);
                }
            }
            catch (Exception)
            {

            }

            T[] dataObjects = (T[])CacheHandler.GetData(ToString(), "GetObjectList", ParameterList.ToArray());

            if (dataObjects != null)
            {
                return dataObjects;
            }

            try
            {
                MySqlCommand Command = new MySqlCommand();
                Command.CommandText = "SELECT * FROM " + tableName;

                Command.CommandText = addOrderBy(Command.CommandText, orderBy, OrderByPropertyName);

                if (LIMIT > 0)
                {
                    Command.CommandText = addLimit(Command.CommandText, LIMIT);
                }

                T[] dataObjectList = GetDataObjects(DatabaseHandler.ExecuteQuery(Command, LogDatabaseStats));
                QueryTrace.RemoveQuery(Command.CommandText);

                CacheHandler.AddToCache(ToString(), "GetObjectList", ParameterList.ToArray(), dataObjectList);
                return dataObjectList;
            }
            catch (Exception e)
            {
                if (exceptionHandler(e, "GetObjectList"))
                {
                    return GetObjectList(LIMIT, orderBy, OrderByPropertyName);
                }
                else
                {
                    return new T[] { };
                }
            }
        }

        protected virtual T[] GetObjectsByIDArray(int[] ID)
        {
            List<string> ParameterList = new List<string>();

            foreach (int i in ID)
            {
                ParameterList.Add(i.ToString());
            }

            T[] dataObjects = (T[])CacheHandler.GetData(ToString(), "GetObjectsByIDArray", ParameterList.ToArray());

            if (dataObjects != null)
            {
                return dataObjects;
            }

            try
            {
                MySqlCommand Command = new MySqlCommand();
                Command.CommandText = "SELECT * FROM " + tableName + " WHERE ";

                for (int i = 0; i < ID.Length; i++)
                {
                    Command.CommandText += "ID = @ID" + i + " OR ";
                    Command.Parameters.AddWithValue("@ID" + i, ID[i]);
                }

                Command.CommandText = Command.CommandText.Substring(0, Command.CommandText.Length - 3);

                T[] dataObjectList = GetDataObjects(DatabaseHandler.ExecuteQuery(Command, LogDatabaseStats));
                QueryTrace.RemoveQuery(Command.CommandText);

                CacheHandler.AddToCache(ToString(), "GetObjectsByIDArray", ParameterList.ToArray(), dataObjectList);
                return dataObjectList;
            }
            catch (Exception e)
            {
                if (exceptionHandler(e, "GetObjectsByIDArray"))
                {
                    return GetObjectsByIDArray(ID);
                }
                else
                {
                    return null;
                }
            }
        }

        protected virtual T[] GetObjectsByChildIDArray(string PropertyName, int[] ID, int LIMIT, OrderBy orderBy = OrderBy.ASC, string OrderByPropertyName = null)
        {
            List<string> ParameterList = new List<string>();

            ParameterList.Add(PropertyName);

            foreach (int i in ID)
            {
                ParameterList.Add(i.ToString());
            }

            ParameterList.Add(LIMIT.ToString());
            ParameterList.Add(orderBy.ToString());

            try
            {
                if (OrderByPropertyName != null)
                {
                    ParameterList.Add(OrderByPropertyName);
                }
            }
            catch (Exception)
            {

            }

            T[] dataObjects = (T[])CacheHandler.GetData(ToString(), "GetObjectsByChildIDArray", ParameterList.ToArray());

            if (dataObjects != null)
            {
                return dataObjects;
            }


            MySqlCommand Command = new MySqlCommand();

            try
            {
                Command.CommandText = "SELECT * FROM " + tableName + " WHERE ";

                for (int i = 0; i < ID.Length; i++)
                {
                    Command.CommandText += PropertyName + " = @" + PropertyName + i + " OR ";
                    Command.Parameters.AddWithValue("@" + PropertyName + i, ID[i]);
                }

                Command.CommandText = Command.CommandText.Substring(0, Command.CommandText.Length - 3);

                Command.CommandText = addOrderBy(Command.CommandText, orderBy, OrderByPropertyName);

                if (LIMIT > 0)
                {
                    Command.CommandText = addLimit(Command.CommandText, LIMIT);
                }

                T[] dataObjectList = GetDataObjects(DatabaseHandler.ExecuteQuery(Command, LogDatabaseStats));
                QueryTrace.RemoveQuery(Command.CommandText);

                CacheHandler.AddToCache(ToString(), "GetObjectsByChildIDArray", ParameterList.ToArray(), dataObjectList);
                return dataObjectList;
            }
            catch (Exception e)
            {
                if (exceptionHandler(e, "GetObjectsByChildIDArray"))
                {
                    return GetObjectsByChildIDArray(PropertyName, ID, LIMIT, orderBy, OrderByPropertyName);
                }
                else
                {
                    return new T[] { };
                }
            }
        }

        protected virtual T[] GetObjectsBySqlQuery(string Query, string[] ParameterNames, object[] Parameters)
        {
            List<string> ParameterList = new List<string>();

            try
            {
                ParameterList.Add(Query);

                foreach (object o in Parameters)
                {
                    ParameterList.Add(o.ToString());
                }
            }
            catch (Exception e)
            {
                if (LogErrors)
                {
                    new EasyMySqlException(this, e);
                }
            }

            T[] dataObjects = (T[])CacheHandler.GetData(ToString(), "CustomQuery", ParameterList.ToArray());

            if (dataObjects != null)
            {
                return dataObjects;
            }

            if (ParameterNames.Count() != Parameters.Count())
            {
                return new T[] { };
            }

            MySqlDataReader reader = null;

            try
            {
                MySqlCommand Command = new MySqlCommand();
                Command.CommandText = Query;

                for (int i = 0; i < ParameterNames.Count(); i++)
                {
                    Command.Parameters.AddWithValue(ParameterNames[i], Parameters[i]);
                }

                reader = DatabaseHandler.ExecuteQuery(Command, LogDatabaseStats);

                T[] dataObjectList = GetDataObjects(reader);
                QueryTrace.RemoveQuery(Command.CommandText);

                CacheHandler.AddToCache(ToString(), "CustomQuery", ParameterList.ToArray(), dataObjectList);
                return dataObjectList;
            }
            catch (InvalidCastException)
            {
                reader.Close();
                CacheHandler.AddToCache(ToString(), "CustomQuery", ParameterList.ToArray(), new List<DataObject>());
                return new T[] { };
            }
            catch (Exception e)
            {
                if (exceptionHandler(e, "Query=" + Query))
                {
                    return GetObjectsBySqlQuery(Query, ParameterNames, Parameters);
                }
                else
                {
                    return new T[] { };
                }
            }
        }

        protected virtual int GetObjectCountWithSqlQuery(string Query, string[] ParameterNames, object[] Parameters)
        {
            List<string> ParameterList = new List<string>();

            try
            {
                ParameterList.Add(Query);

                foreach (object o in Parameters)
                {
                    ParameterList.Add(o.ToString());
                }
            }
            catch (Exception e)
            {
                if (LogErrors)
                {
                    new EasyMySqlException(this, e);
                }
            }

            object Count = CacheHandler.GetData(ToString(), "GetObjectCountWithQuery", ParameterList.ToArray());

            if (Count != null)
            {
                return (int)Count;
            }

            MySqlDataReader reader = null;

            try
            {
                MySqlCommand Command = new MySqlCommand();
                Command.CommandText = Query;

                for (int i = 0; i < ParameterNames.Count(); i++)
                {
                    Command.Parameters.AddWithValue(ParameterNames[i], Parameters[i]);
                }

                reader = DatabaseHandler.ExecuteQuery(Command, LogDatabaseStats);
                int CountFromDatabase = 0;

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        CountFromDatabase = Convert.ToInt32(reader["Count"]);
                    }
                }

                DatabaseHandler.CloseConnectionByReader(reader);

                QueryTrace.RemoveQuery(Command.CommandText);
                CacheHandler.AddToCache(ToString(), "GetCountWithCustomQuery", ParameterList.ToArray(), CountFromDatabase);
                return CountFromDatabase;
            }
            catch (InvalidCastException)
            {
                reader.Close();
                return 0;
            }
            catch (Exception e)
            {
                if (exceptionHandler(e, "GetObjectCountWithQuery=" + Query))
                {
                    return GetObjectCountWithSqlQuery(Query, ParameterNames, Parameters);
                }
                else
                {
                    return 0;
                }
            }
        }

        protected virtual T[] GetObjectsByFilter(Filter filter, int LIMIT = 0, OrderBy orderBy = OrderBy.ASC, string OrderByPropertyName = null)
        {
            return GetObjectsByFilter(new Filter[] { filter }, LIMIT, orderBy, OrderByPropertyName);
        }

        protected virtual T[] GetObjectsByFilter(Filter[] filters, int LIMIT = 0, OrderBy orderBy = OrderBy.ASC, string OrderByPropertyName = null)
        {
            List<string> ParameterList = new List<string>();

            foreach (Filter f in filters)
            {
                foreach (object o in f.values)
                {
                    ParameterList.Add(o.ToString());
                }
            }

            T[] dataObjects = (T[])CacheHandler.GetData(ToString(), "GetObjectsByFilter", ParameterList.ToArray());

            if (dataObjects != null)
            {
                return dataObjects;
            }

            try
            {
                MySqlCommand Command = new MySqlCommand();
                Command.CommandText = "SELECT * FROM " + tableName + " WHERE ";

                foreach (Filter f in filters)
                {
                    Command.CommandText += "(" + f.ToString() + ") AND ";

                    for (int i = 0; i < f.valueNames.Length; i++)
                    {
                        Command.Parameters.AddWithValue(f.valueNames[i], f.values[i]);
                    }
                }

                Command.CommandText = Command.CommandText.Substring(0, Command.CommandText.Length - 4);
                Command.CommandText = addOrderBy(Command.CommandText, orderBy, OrderByPropertyName);

                if (LIMIT > 0)
                {
                    Command.CommandText = addLimit(Command.CommandText, LIMIT);
                }

                T[] dataObjectList = GetDataObjects(DatabaseHandler.ExecuteQuery(Command, LogDatabaseStats));
                QueryTrace.RemoveQuery(Command.CommandText);

                CacheHandler.AddToCache(ToString(), "GetObjectsByFilter", ParameterList.ToArray(), dataObjectList);
                return dataObjectList;
            }
            catch (Exception e)
            {
                if (exceptionHandler(e, "GetObjectsByFilter"))
                {
                    return GetObjectsByFilter(filters, LIMIT, orderBy, OrderByPropertyName);
                }
                else
                {
                    return null;
                }
            }
        }

        protected virtual bool DeleteObject(T Object)
        {
            return DeleteObject(Object.ID);
        }

        protected virtual bool DeleteObject(int ID)
        {
            try
            {
                MySqlCommand Command = new MySqlCommand();
                Command.CommandText = "DELETE FROM " + tableName + " WHERE ID = @ID";
                Command.Parameters.AddWithValue("@ID", ID);

                DatabaseHandler.ExecuteNonQuery(Command, LogDatabaseStats);
                CacheHandler.ClearData(ToString());
                EasyMySqlLog.Log(this, objectType.Name.ToString() + " " + ID + " has been deleted.", logSeverity.Info);

                return true;
            }
            catch (Exception e)
            {
                if (exceptionHandler(e, "DeleteObject=" + ID))
                {
                    return DeleteObject(ID);
                }
                else
                {
                    return false;
                }
            }
        }

        private string addLimit(string Query, int LIMIT)
        {
            return Query + " LIMIT " + LIMIT + " ";
        }

        private string addOrderBy(string Query, OrderBy orderBY, string PropertyName)
        {
            if (PropertyName == null)
            {
                PropertyName = "ID";
            }

            return Query + " ORDER BY " + PropertyName + " " + orderBY.ToString() + " ";
        }

        private bool exceptionHandler(Exception e, string MethodInfo)
        {
            if (e.Message.Contains(" doesn't exist"))
            {
                try
                {
                    return CreateTable();
                }
                catch (Exception ex)
                {
                    if (LogErrors)
                    {
                        new EasyMySqlException(this, ex);
                    }

                    return false;
                }
            }

            if (e.Message.Contains("Could not find specified column in results") || e.Message.StartsWith("Unknown column"))
            {
                try
                {
                    return RestructureTable();
                }
                catch (Exception ex)
                {
                    if (LogErrors)
                    {
                        new EasyMySqlException(this, ex);
                    }

                    return false;
                }
            }

            if (e.Message.StartsWith("Constructor"))
            {
                throw new Exception("Constructor not foud, Please compare field types of " + this.objectType.ToString() + " and the HandlerConstructor");
            }

            if (e.Message.Contains("All Pooled Connections were in use and max pool size was reached"))
            {
                new EasyMySqlException(this, new Exception(QueryTrace.GetTrace()));
            }

            if (LogErrors)
            {
                new EasyMySqlException(this, new InvalidOperationException("Exception has occurred in method: " + MethodInfo));
            }

            return false;
        }

        public override string ToString()
        {
            return GetType().Name;
        }
    }
}
