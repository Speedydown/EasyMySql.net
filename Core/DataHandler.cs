using EasyMySql.Performance;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EasyMySql.Core
{
    public enum OrderBy { ASC, DESC }

    public abstract class DataHandler<T> where T : DataObject
    {
        protected static readonly Field IDField = new Field("ID", typeof(int), 1, true);

        protected bool RegisterError { get; set; }
        protected bool LogDatabaseStats { get; set; }
        protected T DefaultDataObject { get; set; }
        public string tableName { get; private set; }
        protected Field[] fields { get; set; }
        protected Type objectType { get; set; }

        protected DataHandler(string tableName, Field[] fields)
        {
            this.tableName = tableName;
            List<Field> fieldsList = fields.ToList();
            fieldsList.Insert(0, IDField);
            this.fields = fieldsList.ToArray();
            objectType = typeof(T);
        }

        private T[] GetDataObjects(MySqlDataReader Reader)
        {
            List<T> ObjectList = new List<T>();
            List<object> Parameters = new List<object>();

            if (Reader.HasRows)
            {
                try
                {
                    while (Reader.Read())
                    {
                        Parameters = new List<object>();

                        foreach (Field f in fields)
                        {
                            if (f.FieldType == typeof(int))
                            {
                                Parameters.Add(Convert.ToInt32(Reader[f.FieldName]));
                                continue;
                            }

                            if (f.FieldType == typeof(string))
                            {
                                Parameters.Add(Convert.ToString(Reader[f.FieldName]));
                                continue;
                            }

                            if (f.FieldType == typeof(bool))
                            {
                                Parameters.Add(Convert.ToBoolean(Reader[f.FieldName]));
                                continue;
                            }

                            if (f.FieldType == typeof(DateTime))
                            {
                                Parameters.Add(new DateTime((long)Reader[f.FieldName]));
                                continue;
                            }

                            if (f.FieldType == typeof(double))
                            {
                                Parameters.Add(Convert.ToDouble(Reader[f.FieldName]));
                                continue;
                            }
                        }

                        try
                        {
                            ObjectList.Add((T)Activator.CreateInstance(objectType, Parameters.ToArray()));
                        }
                        catch (TargetInvocationException)
                        {
                            //Object is corrupt
                            Console.WriteLine(Parameters[0] + " is corrupt!");
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

            return ObjectList.ToArray();
        }

        private bool CreateTable()
        {
            try
            {
                Field PrimaryKey = null;

                MySqlCommand Command = new MySqlCommand();
                Command.CommandText = "CREATE TABLE IF NOT EXISTS " + tableName + " ( ";
                foreach (Field f in fields)
                {
                    if (f.FieldType == typeof(int))
                    {
                        Command.CommandText += " " + f.FieldName + " INT NOT NULL " + (f.Key ? " AUTO_INCREMENT" : "") + " ,";

                        if (f.Key)
                        {
                            if (PrimaryKey == null)
                            {
                                PrimaryKey = f;
                            }
                            else
                            {
                                throw new Exception("Instance has 2 primary keys");
                            }
                        }
                    }

                    if (f.FieldType == typeof(string))
                    {
                        Command.CommandText += " " + f.FieldName + " VARCHAR(" + f.Size + ") NOT NULL ,";
                    }

                    if (f.FieldType == typeof(bool))
                    {
                        Command.CommandText += " " + f.FieldName + " BIT NOT NULL ,";
                    }

                    if (f.FieldType == typeof(DateTime))
                    {
                        Command.CommandText += " " + f.FieldName + " BIGINT NOT NULL ,";
                    }

                    if (f.FieldType == typeof(Double))
                    {
                        Command.CommandText += " " + f.FieldName + " FLOAT NOT NULL ,";
                    }
                }

                if (PrimaryKey != null)
                {
                    Command.CommandText += "PRIMARY KEY (" + PrimaryKey.FieldName + "), " +
                        "UNIQUE KEY ID_UNIQUE (" + PrimaryKey.FieldName + ") );";
                }
                else
                {
                    Command.CommandText = Command.CommandText.Substring(0, Command.CommandText.Length - 2);
                    Command.CommandText += " );";
                }

                DatabaseHandler.ExecuteNonQuery(Command, this.LogDatabaseStats);

                if (this.DefaultDataObject != null)
                {
                    this.AddObject(this.DefaultDataObject);
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
            Field PrimaryKey = null;

            foreach (Field f in this.fields)
            {
                try
                {
                    MySqlCommand Command = new MySqlCommand();
                    if (f.FieldType == typeof(int))
                    {
                        Command.CommandText = "ALTER TABLE " + tableName + " ADD COLUMN " + f.FieldName + " INT NOT NULL " + (f.Key ? " AUTO_INCREMENT" : "");

                        if (f.Key)
                        {
                            if (PrimaryKey == null)
                            {
                                PrimaryKey = f;
                            }
                            else
                            {
                                throw new Exception("Instance has 2 primary keys");
                            }
                        }
                    }

                    if (f.FieldType == typeof(string))
                    {
                        Command.CommandText = "ALTER TABLE " + tableName + " ADD COLUMN " + f.FieldName + " VARCHAR(" + f.Size + ") NOT NULL ";
                    }

                    if (f.FieldType == typeof(bool))
                    {
                        Command.CommandText = "ALTER TABLE " + tableName + " ADD COLUMN " + f.FieldName + " BIT NOT NULL ";
                    }

                    if (f.FieldType == typeof(DateTime))
                    {
                        Command.CommandText = "ALTER TABLE " + tableName + " ADD COLUMN " + f.FieldName + " BIGINT NOT NULL ";
                    }

                    if (f.FieldType == typeof(double))
                    {
                        Command.CommandText = "ALTER TABLE " + tableName + " ADD COLUMN " + f.FieldName + " FLOAT NOT NULL ";
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
                            if (f.FieldType == typeof(int))
                            {
                                Command.CommandText = "ALTER TABLE " + tableName + " CHANGE COLUMN " + f.FieldName + " " + f.FieldName + " INT NOT NULL " + (f.Key ? " AUTO_INCREMENT" : "");

                                if (f.Key && PrimaryKey == null)
                                {
                                    PrimaryKey = f;
                                }
                                else
                                {
                                    if (PrimaryKey != f && f.Key)
                                    {
                                        throw new Exception("Instance has 2 primary keys");
                                    }
                                }

                            }

                            if (f.FieldType == typeof(string))
                            {
                                Command.CommandText = "ALTER TABLE " + tableName + " CHANGE COLUMN " + f.FieldName + " " + f.FieldName + " VARCHAR(" + f.Size + ") NOT NULL ";
                            }

                            if (f.FieldType == typeof(bool))
                            {
                                Command.CommandText = "ALTER TABLE " + tableName + " CHANGE COLUMN " + f.FieldName + " " + f.FieldName + " BIT NOT NULL ";
                            }

                            if (f.FieldType == typeof(DateTime))
                            {
                                Command.CommandText = "ALTER TABLE " + tableName + " CHANGE COLUMN " + f.FieldName + " " + f.FieldName + " BIGINT NOT NULL ";
                            }

                            if (f.FieldType == typeof(Double))
                            {
                                Command.CommandText = "ALTER TABLE " + tableName + " CHANGE COLUMN " + f.FieldName + " " + f.FieldName + " FLOAT NOT NULL ";
                            }

                            DatabaseHandler.ExecuteNonQuery(Command, this.LogDatabaseStats);
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
                    Command.CommandText = "alter TABLE " + this.tableName + " ADD PRIMARY KEY (`" + PrimaryKey.FieldName + "`);";
                    DatabaseHandler.ExecuteNonQuery(Command, this.LogDatabaseStats);
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("Multiple"))
                    {
                        return true;
                    }

                    if (this.RegisterError)
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
                foreach (Field f in fields)
                {
                    if (f.FieldName != "ID")
                    {
                        Command.CommandText += f.FieldName + ", ";
                    }
                }

                Command.CommandText = Command.CommandText.Substring(0, Command.CommandText.Length - 2);
                Command.CommandText += ") VALUES ( ";

                foreach (Field f in fields)
                {
                    if (f.FieldName != "ID")
                    {
                        Command.CommandText += " @" + f.FieldName + ", ";
                    }
                }

                Command.CommandText = Command.CommandText.Substring(0, Command.CommandText.Length - 2);
                Command.CommandText += " )";

                for (int i = 0; i < fields.Count(); i++)
                {
                    if (fields[i].FieldName != "ID")
                    {
                        if (fields[i].FieldType != typeof(DateTime))
                        {
                            object FieldValue = DataObject.GetType().GetProperty(fields[i].FieldName).GetValue(DataObject, null);

                            Command.Parameters.AddWithValue("@" + fields[i].FieldName, FieldValue);

                            if (fields[i].FieldType == typeof(string) && FieldValue.ToString().Length > fields[i].Size)
                            {
                                EasyMySqlLog.Log(this, "Field " + fields[i].FieldName + " has exceeded its size, Consider increasing its fieldsize. Do not forgot to restruct the table later.", logSeverity.Warning);
                            }
                        }
                        else
                        {
                            DateTime dt = (DateTime)DataObject.GetType().GetProperty(fields[i].FieldName).GetValue(DataObject, null);
                            Command.Parameters.AddWithValue("@" + fields[i].FieldName, dt.Ticks);
                        }
                    }
                }

                DatabaseHandler.ExecuteNonQuery(Command, LogDatabaseStats);

                if (RegisterError)
                {
                    EasyMySqlLog.Log(this, DataObject.ToString() + " with id " + Command.LastInsertedId + " has been added.", logSeverity.Info);
                }

                DataObject.setID(Convert.ToInt32(Command.LastInsertedId));
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
                throw new Exception("Value 0 is not allowed.");
            }

            try
            {
                MySqlCommand Command = new MySqlCommand();
                Command.CommandText = "UPDATE " + tableName + " SET ";

                foreach (Field f in this.fields)
                {
                    if (f.FieldName != "ID")
                    {
                        Command.CommandText += f.FieldName + " = @" + f.FieldName + ", ";

                        foreach (PropertyInfo pi in props)
                        {
                            if (pi.Name == f.FieldName)
                            {
                                if (pi.PropertyType == typeof(DateTime))
                                {
                                    DateTime dt = (DateTime)pi.GetValue(dataObject, null);

                                    Command.Parameters.AddWithValue("@" + f.FieldName, dt.Ticks);
                                    break;
                                }
                                else
                                {
                                    Command.Parameters.AddWithValue("@" + f.FieldName, pi.GetValue(dataObject, null));
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

                if (RegisterError)
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
                T dataObject = (T)CacheHandler.GetData(this.ToString(), "GetObjectByID", new string[] { Convert.ToString(ID) });

                if (dataObject != null)
                {
                    return dataObject;
                }

                MySqlCommand Command = new MySqlCommand();
                Command.CommandText = "SELECT * FROM " + this.tableName + " WHERE ID = @ID";
                Command.Parameters.AddWithValue("@ID", ID);

                T[] ObjectList = GetDataObjects(DatabaseHandler.ExecuteQuery(Command, this.LogDatabaseStats));
                QueryTrace.RemoveQuery(Command.CommandText);

                if (ObjectList.Count() > 0)
                {
                    CacheHandler.AddToCache(this.ToString(), "GetObjectByID", new string[] { Convert.ToString(ID) }, ObjectList[0]);
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

        protected virtual T[] GetObjectByFieldsAndSearchQuery(Field SearchField, string SearchQuery, bool Exact = false, int LIMIT = 0, OrderBy orderBy = OrderBy.ASC, Field orderByField = null)
        {
            return GetObjectByFieldsAndSearchQuery(new Field[] { SearchField }, SearchQuery, Exact, LIMIT, orderBy, orderByField);
        }

        protected virtual T[] GetObjectByFieldsAndSearchQuery(Field[] SearchFields, string SearchQuery, bool Exact = false, int LIMIT = 0, OrderBy orderBy = OrderBy.ASC, Field orderByField = null)
        {
            List<string> ParameterList = new List<string>();

            foreach (Field f in fields)
            {
                ParameterList.Add(f.FieldName);
            }

            ParameterList.Add(SearchQuery);

            ParameterList.Add(Exact.ToString());
            ParameterList.Add(LIMIT.ToString());
            ParameterList.Add(orderBy.ToString());

            try
            {
                if (orderByField != null)
                {
                    ParameterList.Add(orderByField.FieldName);
                }
            }
            catch (Exception)
            {

            }

            T[] dataObjects = (T[])CacheHandler.GetData(this.ToString(), "GetObjectByFieldsAndSearchQuery", ParameterList.ToArray());

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
                Command.CommandText = "SELECT * FROM " + this.tableName + " WHERE ";

                foreach (Field f in SearchFields)
                {
                    Command.CommandText += f.FieldName + " LIKE @QUERY OR ";
                }

                Command.Parameters.AddWithValue("@QUERY", SQLQuery);

                if (SearchFields.Count() > 0)
                {
                    Command.CommandText = Command.CommandText.Substring(0, Command.CommandText.Length - 3);
                }

                Command.CommandText = this.addOrderBy(Command.CommandText, orderBy, orderByField);

                if (LIMIT > 0)
                {
                    Command.CommandText = this.addLimit(Command.CommandText, LIMIT);
                }

                T[] dataObjectList = GetDataObjects(DatabaseHandler.ExecuteQuery(Command, this.LogDatabaseStats));
                QueryTrace.RemoveQuery(Command.CommandText);

                CacheHandler.AddToCache(this.ToString(), "GetObjectByFieldsAndSearchQuery", ParameterList.ToArray(), dataObjectList);
                return dataObjectList;
            }
            catch (Exception e)
            {
                if (this.exceptionHandler(e, "GetObjectByFieldsAndSearchQuery=" + SearchQuery))
                {
                    return this.GetObjectByFieldsAndSearchQuery(SearchFields, SearchQuery, Exact, LIMIT, orderBy, orderByField);
                }
                else
                {
                    return new T[] { };
                }
            }
        }

        protected virtual T[] GetObjectsByChildID(Field Child, int ID, int LIMIT, OrderBy orderBy = OrderBy.ASC, Field orderByField = null)
        {
            List<string> ParameterList = new List<string>();

            ParameterList.Add(Child.FieldName);
            ParameterList.Add(ID.ToString());
            ParameterList.Add(LIMIT.ToString());
            ParameterList.Add(orderBy.ToString());

            try
            {
                if (orderByField != null)
                {
                    ParameterList.Add(orderByField.FieldName);
                }
            }
            catch (Exception)
            {

            }

            T[] dataObjects = (T[])CacheHandler.GetData(this.ToString(), "GetObjectsByChildID", ParameterList.ToArray());

            if (dataObjects != null)
            {
                return dataObjects;
            }

            MySqlCommand Command = new MySqlCommand();

            try
            {
                Command.CommandText = "SELECT * FROM " + this.tableName + " WHERE " + Child.FieldName + " = @" + Child.FieldName;
                Command.Parameters.AddWithValue("@" + Child.FieldName, ID);

                Command.CommandText = this.addOrderBy(Command.CommandText, orderBy, orderByField);

                if (LIMIT > 0)
                {
                    Command.CommandText = this.addLimit(Command.CommandText, LIMIT);
                }

                T[] dataObjectList = GetDataObjects(DatabaseHandler.ExecuteQuery(Command, this.LogDatabaseStats));
                QueryTrace.RemoveQuery(Command.CommandText);

                CacheHandler.AddToCache(this.ToString(), "GetObjectsByChildID", ParameterList.ToArray(), dataObjectList);
                return dataObjectList;
            }
            catch (Exception e)
            {
                if (this.exceptionHandler(e, "GetObjectsByChildID=" + Child.FieldName + ":" + ID))
                {
                    return GetObjectsByChildID(Child, ID, LIMIT, orderBy, orderByField);
                }
                else
                {
                    return new T[] { };
                }
            }
        }

        protected virtual T[] GetObjectList(int LIMIT = 0, OrderBy orderBy = OrderBy.ASC, Field orderByField = null)
        {
            List<string> ParameterList = new List<string>();

            ParameterList.Add(LIMIT.ToString());
            ParameterList.Add(orderBy.ToString());

            try
            {
                if (orderByField != null)
                {
                    ParameterList.Add(orderByField.FieldName);
                }
            }
            catch (Exception)
            {

            }

            T[] dataObjects = (T[])CacheHandler.GetData(this.ToString(), "GetObjectList", ParameterList.ToArray());

            if (dataObjects != null)
            {
                return dataObjects;
            }

            try
            {
                MySqlCommand Command = new MySqlCommand();
                Command.CommandText = "SELECT * FROM " + this.tableName;

                Command.CommandText = this.addOrderBy(Command.CommandText, orderBy, orderByField);

                if (LIMIT > 0)
                {
                    Command.CommandText = this.addLimit(Command.CommandText, LIMIT);
                }

                T[] dataObjectList = GetDataObjects(DatabaseHandler.ExecuteQuery(Command, this.LogDatabaseStats));
                QueryTrace.RemoveQuery(Command.CommandText);

                CacheHandler.AddToCache(this.ToString(), "GetObjectList", ParameterList.ToArray(), dataObjectList);
                return dataObjectList;
            }
            catch (Exception e)
            {
                if (this.exceptionHandler(e, "GetObjectList"))
                {
                    return this.GetObjectList(LIMIT, orderBy, orderByField);
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

        protected virtual T[] GetObjectsByChildIDArray(Field Child, int[] ID, int LIMIT, OrderBy orderBy = OrderBy.ASC, Field orderByField = null)
        {
            List<string> ParameterList = new List<string>();

            ParameterList.Add(Child.FieldName);

            foreach (int i in ID)
            {
                ParameterList.Add(i.ToString());
            }

            ParameterList.Add(LIMIT.ToString());
            ParameterList.Add(orderBy.ToString());

            try
            {
                if (orderByField != null)
                {
                    ParameterList.Add(orderByField.FieldName);
                }
            }
            catch (Exception)
            {

            }

            T[] dataObjects = (T[])CacheHandler.GetData(this.ToString(), "GetObjectsByChildIDArray", ParameterList.ToArray());

            if (dataObjects != null)
            {
                return dataObjects;
            }


            MySqlCommand Command = new MySqlCommand();

            try
            {
                Command.CommandText = "SELECT * FROM " + this.tableName + " WHERE ";

                for (int i = 0; i < ID.Length; i++)
                {
                    Command.CommandText += Child.FieldName + " = @" + Child.FieldName + i + " OR ";
                    Command.Parameters.AddWithValue("@" + Child.FieldName + i, ID[i]);
                }

                Command.CommandText = Command.CommandText.Substring(0, Command.CommandText.Length - 3);

                Command.CommandText = this.addOrderBy(Command.CommandText, orderBy, orderByField);

                if (LIMIT > 0)
                {
                    Command.CommandText = this.addLimit(Command.CommandText, LIMIT);
                }

                T[] dataObjectList = GetDataObjects(DatabaseHandler.ExecuteQuery(Command, this.LogDatabaseStats));
                QueryTrace.RemoveQuery(Command.CommandText);

                CacheHandler.AddToCache(this.ToString(), "GetObjectsByChildIDArray", ParameterList.ToArray(), dataObjectList);
                return dataObjectList;
            }
            catch (Exception e)
            {
                if (this.exceptionHandler(e, "GetObjectsByChildIDArray"))
                {
                    return GetObjectsByChildIDArray(Child, ID, LIMIT, orderBy, orderByField);
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
                if (RegisterError)
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
                if (RegisterError)
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

                reader = DatabaseHandler.ExecuteQuery(Command, this.LogDatabaseStats);
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
                CacheHandler.AddToCache(this.ToString(), "GetCountWithCustomQuery", ParameterList.ToArray(), CountFromDatabase);
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

        protected virtual T[] GetObjectsByFilter(Filter filter, int LIMIT = 0, OrderBy orderBy = OrderBy.ASC, Field orderByField = null)
        {
            return GetObjectsByFilter(new Filter[] { filter }, LIMIT, orderBy, orderByField);
        }

        protected virtual T[] GetObjectsByFilter(Filter[] filters, int LIMIT = 0, OrderBy orderBy = OrderBy.ASC, Field orderByField = null)
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
                Command.CommandText = addOrderBy(Command.CommandText, orderBy, orderByField);

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
                    return GetObjectsByFilter(filters, LIMIT, orderBy, orderByField);
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

        private string addOrderBy(string Query, OrderBy orderBY, Field orderByField)
        {
            if (orderByField == null)
            {
                orderByField = this.fields[0];
            }

            return Query + " ORDER BY " + orderByField.FieldName + " " + orderBY.ToString() + " ";
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
                    if (RegisterError)
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
                    if (RegisterError)
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

            if (RegisterError)
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
