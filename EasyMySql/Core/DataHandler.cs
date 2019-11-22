using EasyMySql.Attributes;
using EasyMySql.Performance;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace EasyMySql.Core
{
  public enum OrderBy { ASC, DESC }

  public class DataHandler<T> : IDisposable where T : DataObject, new()
  {
    protected bool LogErrors { get; set; }
    protected bool LogDatabaseStats { get; set; }
    protected T DefaultDataObject { get; set; }
    public string TableName { get; protected set; }
    private DataObjectPropertyInfo[] PropertyInfo { get; set; }
    protected Type objectType { get; set; }
    private bool HasRestructered = false;

    public DataHandler()
    {
      objectType = typeof(T);
      TableName = objectType.Name;
      PropertyInfo = GetPropertyInfo();
    }

    public DataHandler(string tablename)
    {
      objectType = typeof(T);
      TableName = tablename;
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
              EasyMySqlLog.Log(this, "Could not get dataobjects from table: " + TableName, logSeverity.Error);

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
          bool IsUnique = false;
          int StringLength = 250;

          foreach (object attribute in pi.GetCustomAttributes(true))
          {
            if (attribute is IgnoreAttribute)
            {
              HasIgnoreAttribute = true;
              break;
            }

            if (attribute is KeyAttribute)
            {
              IsPrimaryKey = true;
            }

            if (attribute is UniqueAttribute)
            {
              IsUnique = true;
            }

            if (pi.PropertyType == typeof(string) && attribute is LengthAttribute)
            {
              StringLength = (attribute as LengthAttribute).Length;
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
            IsUnique = IsUnique,
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
        Command.CommandText = "CREATE TABLE IF NOT EXISTS " + TableName + " ( ";

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
          else if (pi.Type.IsEnum)
          {
            Command.CommandText += " " + pi.Name + " INT NOT NULL ,";
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
              "UNIQUE KEY ID_UNIQUE (" + PrimaryKey.Name + " ) );";
        }
        else
        {
          Command.CommandText = Command.CommandText.Substring(0, Command.CommandText.Length - 2);
          Command.CommandText += " );";
        }

        DatabaseHandler.ExecuteNonQuery(Command, LogDatabaseStats);
        RecreateUniqueKeys();

        if (DefaultDataObject != null)
        {
          Add(DefaultDataObject);
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

    protected virtual bool RestructureTable()
    {
      try
      {
        DataObjectPropertyInfo PrimaryKey = null;

        foreach (DataObjectPropertyInfo pi in PropertyInfo)
        {
          try
          {
            MySqlCommand Command = new MySqlCommand();
            if (pi.Type == typeof(int))
            {
              Command.CommandText = "ALTER TABLE " + TableName + " ADD COLUMN " + pi.Name + " INT NOT NULL " + (pi.IsPrimaryKey ? " AUTO_INCREMENT" : "");

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

            if (pi.Type.IsEnum)
            {
              Command.CommandText = "ALTER TABLE " + TableName + " ADD COLUMN " + pi.Name + " INT NOT NULL ";
            }

            if (pi.Type == typeof(string))
            {
              Command.CommandText = "ALTER TABLE " + TableName + " ADD COLUMN " + pi.Name + " VARCHAR(" + pi.Length + ") NOT NULL ";
            }

            if (pi.Type == typeof(bool))
            {
              Command.CommandText = "ALTER TABLE " + TableName + " ADD COLUMN " + pi.Name + " BIT NOT NULL ";
            }

            if (pi.Type == typeof(DateTime))
            {
              Command.CommandText = "ALTER TABLE " + TableName + " ADD COLUMN " + pi.Name + " BIGINT NOT NULL ";
            }

            if (pi.Type == typeof(double))
            {
              Command.CommandText = "ALTER TABLE " + TableName + " ADD COLUMN " + pi.Name + " FLOAT NOT NULL ";
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
                  Command.CommandText = "ALTER TABLE " + TableName + " CHANGE COLUMN " + pi.Name + " " + pi.Name + " INT NOT NULL " + (pi.IsPrimaryKey ? " AUTO_INCREMENT" : "");

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
                  Command.CommandText = "ALTER TABLE " + TableName + " CHANGE COLUMN " + pi.Name + " " + pi.Name + " VARCHAR(" + pi.Length + ") NOT NULL ";
                }

                if (pi.Type.IsEnum)
                {
                  Command.CommandText = "ALTER TABLE " + TableName + " CHANGE COLUMN " + pi.Name + " " + pi.Name + " INT NOT NULL ";
                }

                if (pi.Type == typeof(bool))
                {
                  Command.CommandText = "ALTER TABLE " + TableName + " CHANGE COLUMN " + pi.Name + " " + pi.Name + " BIT NOT NULL ";
                }

                if (pi.Type == typeof(DateTime))
                {
                  Command.CommandText = "ALTER TABLE " + TableName + " CHANGE COLUMN " + pi.Name + " " + pi.Name + " BIGINT NOT NULL ";
                }

                if (pi.Type == typeof(double))
                {
                  Command.CommandText = "ALTER TABLE " + TableName + " CHANGE COLUMN " + pi.Name + " " + pi.Name + " FLOAT NOT NULL ";
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
            Command.CommandText = "alter TABLE " + TableName + " ADD PRIMARY KEY (`" + PrimaryKey.Name + "`);";
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
      finally
      {
        RecreateUniqueKeys();
      }
    }

    private void RecreateUniqueKeys()
    {
      if (this is UniqueKeyHandler)
      {
        return;
      }

      List<DataObjectPropertyInfo> UniqueColumns = PropertyInfo.Where(p => p.IsUnique).ToList();

      foreach (UniqueKey uKey in UniqueKeyHandler.Instance.GetItems().Where(o => o.TableName == TableName))
      {
        if (UniqueColumns.Count(o => o.Name == uKey.ColumnName) == 0)
        {
          MySqlCommand Command = new MySqlCommand();
          Command.CommandText = string.Format("DROP INDEX {1} ON {0};", TableName, uKey.IndexName);

          DatabaseHandler.ExecuteNonQuery(Command, LogDatabaseStats);
          UniqueKeyHandler.Instance.Delete(uKey);
        }
      }

      if (UniqueColumns.Count > 0)
      {
        try
        {
          foreach (var uColumn in UniqueColumns)
          {
            if (UniqueKeyHandler.Instance.GetItems().Count(o => o.TableName == TableName && o.ColumnName == uColumn.Name) == 0)
            {
              UniqueKey uKey = new UniqueKey()
              {
                TableName = TableName,
                ColumnName = uColumn.Name,
                IndexName = "u" + uColumn.Name,
              };

              MySqlCommand Command = new MySqlCommand();
              Command.CommandText = string.Format("CREATE UNIQUE INDEX {1} ON {0}({2});", TableName, uKey.IndexName, uKey.ColumnName);

              DatabaseHandler.ExecuteNonQuery(Command, LogDatabaseStats);

              UniqueKeyHandler.Instance.Add(uKey);
            }
          }
        }
        catch
        {

        }
      }
    }

    public virtual T Save(T DataObject)
    {
      if (DataObject.ID == 0)
      {
        return Add(DataObject);
      }
      else
      {
        return Update(DataObject);
      }
    }

    public virtual IEnumerable<T> Save(IEnumerable<T> DataObjects)
    {
      if (DataObjects == null || DataObjects.Count() == 0)
      {
        return null;
      }

      Update(DataObjects.Where(d => d.ID != 0));
      var NewDataObjects = DataObjects.Where(d => d.ID == 0);

      foreach (T Object in DataObjects)
      {
        Save(Object);
      }

      return DataObjects;
    }

    protected virtual MySqlCommand GetAddQuery(T dataObject)
    {
      BeforeQuery();
      dataObject.TrimValues();


      MySqlCommand Command = new MySqlCommand();
      Command.CommandText = "INSERT INTO " + TableName + " ( ";

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
            object FieldValue = dataObject.GetType().GetProperty(PropertyInfo[i].Name).GetValue(dataObject, null);

            Command.Parameters.AddWithValue("@" + PropertyInfo[i].Name, FieldValue);

            if (PropertyInfo[i].Type == typeof(string) && FieldValue != null && FieldValue.ToString().Length > PropertyInfo[i].Length)
            {
              EasyMySqlLog.Log(this, "Field " + PropertyInfo[i].Name + " has exceeded its size, Consider increasing its fieldsize. Do not forgot to force a restruct later.", logSeverity.Warning);
            }
          }
          else
          {
            DateTime dt = (DateTime)dataObject.GetType().GetProperty(PropertyInfo[i].Name).GetValue(dataObject, null);
            Command.Parameters.AddWithValue("@" + PropertyInfo[i].Name, dt.Ticks);
          }
        }
      }

      return Command;
    }

    public virtual T Add(T DataObject)
    {
      try
      {
        var commmand = GetAddQuery(DataObject);

        DatabaseHandler.ExecuteNonQuery(commmand, LogDatabaseStats);

        if (LogErrors)
        {
          EasyMySqlLog.Log(this, DataObject.ToString() + " with id " + commmand.LastInsertedId + " has been added.", logSeverity.Info);
        }

        DataObject.ID = (Convert.ToInt32(commmand.LastInsertedId));
        CacheHandler.ClearData(ToString());
        return DataObject;
      }
      catch (Exception e)
      {
        if (exceptionHandler(e, "AddObject"))
        {
          return Add(DataObject);
        }
        else
        {
          Debug.WriteLine(e.ToString());
          return null;
        }

      }
    }

    public virtual T Update(T DataObject)
    {
      BeforeQuery();
      DataObject.TrimValues();

      IList<PropertyInfo> props = new List<PropertyInfo>(objectType.GetProperties());

      if (DataObject.ID == 0)
      {
        throw new Exception("ID can not be 0. try adding this object first.");
      }

      try
      {
        MySqlCommand Command = new MySqlCommand();
        Command.CommandText = "UPDATE " + TableName + " SET ";

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
                  DateTime dt = (DateTime)opi.GetValue(DataObject, null);

                  Command.Parameters.AddWithValue("@" + pi.Name, dt.Ticks);
                  break;
                }
                else
                {
                  Command.Parameters.AddWithValue("@" + pi.Name, opi.GetValue(DataObject, null));
                  break;
                }
              }
            }
          }
        }

        Command.CommandText = Command.CommandText.Substring(0, Command.CommandText.Length - 2);

        Command.CommandText += " WHERE ID = @ID";
        Command.Parameters.AddWithValue("ID", DataObject.ID);

        DatabaseHandler.ExecuteNonQuery(Command, LogDatabaseStats);

        if (LogErrors)
        {
          EasyMySqlLog.Log(this, DataObject.ToString() + " with id " + DataObject.ID + " has been updated.", logSeverity.Info);
        }

        CacheHandler.ClearData(ToString());

        return DataObject;
      }
      catch (Exception e)
      {
        if (exceptionHandler(e, "UpdateObject"))
        {
          return Update(DataObject);
        }
        else
        {
          return null;
        }
      }
    }

    public virtual IEnumerable<T> Update(IEnumerable<T> DataObjects)
    {
      BeforeQuery();

      if (DataObjects == null || DataObjects.Count() == 0)
      {
        return null;
      }

      MySqlCommand Command = new MySqlCommand();
      StringBuilder LogText = new StringBuilder();
      Command.CommandText = string.Empty;

      foreach (T dataObject in DataObjects)
      {
        dataObject.TrimValues();

        IList<PropertyInfo> props = new List<PropertyInfo>(objectType.GetProperties());

        if (dataObject.ID == 0)
        {
          throw new Exception("ID can not be 0. try adding this object first.");
        }

        string CommandText = "UPDATE " + TableName + " SET ";

        foreach (DataObjectPropertyInfo pi in PropertyInfo)
        {
          if (pi.Name != "ID")
          {
            CommandText += pi.Name + " = @" + pi.Name + dataObject.ID + ", ";

            foreach (PropertyInfo opi in props)
            {
              if (opi.Name == pi.Name)
              {
                if (opi.PropertyType == typeof(DateTime))
                {
                  DateTime dt = (DateTime)opi.GetValue(dataObject, null);

                  Command.Parameters.AddWithValue("@" + pi.Name + dataObject.ID, dt.Ticks);
                  break;
                }
                else
                {
                  Command.Parameters.AddWithValue("@" + pi.Name + dataObject.ID, opi.GetValue(dataObject, null));
                  break;
                }
              }
            }
          }
        }

        CommandText = CommandText.Substring(0, CommandText.Length - 2);

        CommandText += " WHERE ID = @ID" + dataObject.ID;
        Command.Parameters.AddWithValue("ID" + dataObject.ID, dataObject.ID);

        CommandText += ";";
        Command.CommandText += CommandText;
        LogText.AppendLine(dataObject.ToString() + " with id " + dataObject.ID + " has been updated.");
      }

      try
      {
        DatabaseHandler.ExecuteNonQuery(Command, LogDatabaseStats);

        if (LogErrors)
        {
          EasyMySqlLog.Log(this, LogText.ToString(), logSeverity.Info);
        }

        CacheHandler.ClearData(ToString());

        return DataObjects;
      }
      catch (Exception e)
      {
        if (exceptionHandler(e, "UpdateObjects"))
        {
          return Update(DataObjects);
        }
        else
        {
          return null;
        }
      }
    }

    /// <summary>
    /// Gets an item by ID
    /// </summary>
    /// <param name="ID"></param>
    /// <returns>Item of type <typeparamref name="T"/></returns>
    public virtual T GetItem(int ID)
    {
      try
      {
        BeforeQuery();

        T dataObject = (T)CacheHandler.GetData(ToString(), "GetItem", new string[] { Convert.ToString(ID) });

        if (dataObject != null)
        {
          return dataObject;
        }

        MySqlCommand Command = new MySqlCommand();
        Command.CommandText = "SELECT * FROM " + TableName + " WHERE ID = @ID";
        Command.Parameters.AddWithValue("@ID", ID);

        T[] ObjectList = GetDataObjects(DatabaseHandler.ExecuteQuery(Command, LogDatabaseStats));
        QueryTrace.RemoveQuery(Command.CommandText);

        if (ObjectList.Count() > 0)
        {
          CacheHandler.AddToCache(ToString(), "GetItem", new string[] { Convert.ToString(ID) }, ObjectList[0]);
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
        if (exceptionHandler(e, "GetItem=" + ID))
        {
          return GetItem(ID);
        }
        else
        {
          return null;
        }
      }
    }

    /// <summary>
    /// Searches table for object that match the input ID (int) and returns the first match.
    /// </summary>
    /// <param name="IDPropertyName">Property names of property in type T</param>
    /// <param name="ChildID">ID of child object</param>
    /// <param name="LIMIT"></param>
    /// <param name="orderBy"></param>
    /// <param name="OrderByPropertyName"></param>
    /// <returns>Item of type <typeparamref name="T"/></returns>
    protected virtual T GetItem(string IDPropertyName, int ChildID, int LIMIT, OrderBy orderBy = OrderBy.ASC, string OrderByPropertyName = null)
    {
      return GetItems(IDPropertyName, ChildID, LIMIT, orderBy, OrderByPropertyName).FirstOrDefault();
    }

    /// <summary>
    /// Gets the first matching item of type <typeparamref name="T"/> by SQL.
    /// </summary>
    /// <param name="SqlQuery"></param>
    /// <param name="ParameterNames"></param>
    /// <param name="Parameters"></param>
    /// <returns>Item of type <typeparamref name="T"/></returns>
    protected virtual T GetItem(string SqlQuery, string[] ParameterNames, object[] Parameters)
    {
      return GetItems(SqlQuery, ParameterNames, Parameters).FirstOrDefault();
    }

    /// <summary>
    /// Searches table for objects that match the searchquery.
    /// </summary>
    /// <param name="PropertyNames">Property name of property in type T</param>
    /// <param name="SearchQuery">Keyword</param>
    /// <param name="Exact"></param>
    /// <param name="LIMIT"></param>
    /// <param name="orderBy"></param>
    /// <param name="OrderByPropertyName"></param>
    /// <returns>List of items of type <typeparamref name="T"/> that match the input query</returns>
    protected virtual T[] GetItems(string PropertyName, string SearchQuery, bool Exact = false, int LIMIT = 0, OrderBy orderBy = OrderBy.ASC, string OrderByPropertyName = null)
    {
      return GetItems(new string[] { PropertyName }, SearchQuery, Exact, LIMIT, orderBy, OrderByPropertyName);
    }

    /// <summary>
    /// Searches table for objects that match the searchquery.
    /// </summary>
    /// <param name="PropertyNames">Property names of property in type T</param>
    /// <param name="SearchQuery">Keyword</param>
    /// <param name="Exact"></param>
    /// <param name="LIMIT"></param>
    /// <param name="orderBy"></param>
    /// <param name="OrderByPropertyName"></param>
    /// <returns>List of items of type <typeparamref name="T"/> that match the input query</returns>
    protected virtual T[] GetItems(string[] PropertyNames, string SearchQuery, bool Exact = false, int LIMIT = 0, OrderBy orderBy = OrderBy.ASC, string OrderByPropertyName = null)
    {
      BeforeQuery();

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

      T[] dataObjects = (T[])CacheHandler.GetData(ToString(), "GetItemsBySearchQuery", ParameterList.ToArray());

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
        Command.CommandText = "SELECT * FROM " + TableName + " WHERE ";

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

        CacheHandler.AddToCache(ToString(), "GetItemsBySearchQuery", ParameterList.ToArray(), dataObjectList);
        return dataObjectList;
      }
      catch (Exception e)
      {
        if (exceptionHandler(e, "GetItemsBySearchQuery=" + SearchQuery))
        {
          return GetItems(PropertyNames, SearchQuery, Exact, LIMIT, orderBy, OrderByPropertyName);
        }
        else
        {
          return new T[] { };
        }
      }
    }

    /// <summary>
    /// Searches table for objects that match the input ID (int) and returns those
    /// </summary>
    /// <param name="IDPropertyName">Property names of property in type T</param>
    /// <param name="ChildID">ID of child object</param>
    /// <param name="LIMIT"></param>
    /// <param name="orderBy"></param>
    /// <param name="OrderByPropertyName"></param>
    /// <returns>List of items of type <typeparamref name="T"/></returns>
    protected virtual T[] GetItems(string IDPropertyName, int ChildID, int LIMIT, OrderBy orderBy = OrderBy.ASC, string OrderByPropertyName = null)
    {
      BeforeQuery();

      List<string> ParameterList = new List<string>();

      ParameterList.Add(IDPropertyName);
      ParameterList.Add(ChildID.ToString());
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
        Command.CommandText = "SELECT * FROM " + TableName + " WHERE " + IDPropertyName + " = @" + IDPropertyName;
        Command.Parameters.AddWithValue("@" + IDPropertyName, ChildID);

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
        if (exceptionHandler(e, "GetObjectsByChildID=" + IDPropertyName + ":" + ChildID))
        {
          return GetItems(IDPropertyName, ChildID, LIMIT, orderBy, OrderByPropertyName);
        }
        else
        {
          return new T[] { };
        }
      }
    }

    /// <summary>
    /// Returns all items in this table.
    /// </summary>
    /// <param name="LIMIT"></param>
    /// <param name="orderBy"></param>
    /// <param name="OrderByPropertyName"></param>
    /// <returns>List of items of type <typeparamref name="T"/></returns>
    public virtual T[] GetItems(int LIMIT = 0, OrderBy orderBy = OrderBy.ASC, string OrderByPropertyName = null)
    {
      BeforeQuery();

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
        Command.CommandText = "SELECT * FROM " + TableName;

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
          return GetItems(LIMIT, orderBy, OrderByPropertyName);
        }
        else
        {
          return new T[] { };
        }
      }
    }

    /// <summary>
    /// Returns items with the given input id's
    /// </summary>
    /// <param name="ID"></param>
    /// <returns>List of items of type <typeparamref name="T"/></returns>
    protected virtual T[] GetItems(IEnumerable<int> ID)
    {
      BeforeQuery();

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
        Command.CommandText = "SELECT * FROM " + TableName + " WHERE ";

        for (int i = 0; i < ID.Count(); i++)
        {
          Command.CommandText += "ID = @ID" + i + " OR ";
          Command.Parameters.AddWithValue("@ID" + i, ID.ElementAt(i));
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
          return GetItems(ID);
        }
        else
        {
          return null;
        }
      }
    }

    /// <summary>
    /// Searches table for objects that match one of the input IDs (int) and returns those
    /// </summary>
    /// <param name="IDPropertyName">Property names of property in type T</param>
    /// <param name="ChildIDs">List of child IDs</param>
    /// <param name="LIMIT"></param>
    /// <param name="orderBy"></param>
    /// <param name="OrderByPropertyName"></param>
    /// <returns>List of items of type <typeparamref name="T"/></returns>
    protected virtual T[] GetItems(string IDPropertyName, int[] ChildIDs, int LIMIT, OrderBy orderBy = OrderBy.ASC, string OrderByPropertyName = null)
    {
      BeforeQuery();

      List<string> ParameterList = new List<string>();

      ParameterList.Add(IDPropertyName);

      foreach (int i in ChildIDs)
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
        Command.CommandText = "SELECT * FROM " + TableName + " WHERE ";

        for (int i = 0; i < ChildIDs.Length; i++)
        {
          Command.CommandText += IDPropertyName + " = @" + IDPropertyName + i + " OR ";
          Command.Parameters.AddWithValue("@" + IDPropertyName + i, ChildIDs[i]);
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
          return GetItems(IDPropertyName, ChildIDs, LIMIT, orderBy, OrderByPropertyName);
        }
        else
        {
          return new T[] { };
        }
      }
    }

    /// <summary>
    /// Gets items of type <typeparamref name="T"/> by SQL.
    /// </summary>
    /// <param name="SqlQuery"></param>
    /// <param name="ParameterNames"></param>
    /// <param name="Parameters"></param>
    /// <returns>List of items of type <typeparamref name="T"/></returns>
    protected virtual T[] GetItems(string SqlQuery, string[] ParameterNames, object[] Parameters)
    {
      BeforeQuery();

      List<string> ParameterList = new List<string>();

      try
      {
        ParameterList.Add(SqlQuery);

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
        Command.CommandText = SqlQuery;

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
        if (exceptionHandler(e, "Query=" + SqlQuery))
        {
          return GetItems(SqlQuery, ParameterNames, Parameters);
        }
        else
        {
          return new T[] { };
        }
      }
    }

    protected virtual void ExecuteNonQuery(string Query, string[] ParameterNames, object[] Parameters)
    {
      try
      {
        MySqlCommand Command = new MySqlCommand();
        Command.CommandText = Query;

        for (int i = 0; i < ParameterNames.Count(); i++)
        {
          Command.Parameters.AddWithValue(ParameterNames[i], Parameters[i]);
        }

        DatabaseHandler.ExecuteNonQuery(Command, LogDatabaseStats);
        QueryTrace.RemoveQuery(Command.CommandText);
        CacheHandler.ClearData(GetType().Name);

      }
      catch (Exception e)
      {
        if (exceptionHandler(e, "ExecuteNonQuery=" + Query))
        {
          ExecuteNonQuery(Query, ParameterNames, Parameters);
        }
      }
    }

    /// <summary>
    /// Counts Items by SQL Query.
    /// </summary>
    /// <param name="Query">Example: select Count(*) as Count from Users where ID = @ID;
    /// Note: Result column has to be named "Count"</param>
    /// <param name="ParameterNames">Example: @ID</param>
    /// <param name="Parameters">Example: 1</param>
    /// <returns>Count of items matching the SQL query</returns>
    protected virtual int CountItems(string Query, string[] ParameterNames, object[] Parameters)
    {
      BeforeQuery();

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
          return CountItems(Query, ParameterNames, Parameters);
        }
        else
        {
          return 0;
        }
      }
    }

    /// <summary>
    /// Counts all items in the Table
    /// </summary>
    /// <returns></returns>
    public virtual int CountItems()
    {
      BeforeQuery();

      object Count = CacheHandler.GetData(ToString(), "CountItems", new string[] { });

      if (Count != null)
      {
        return (int)Count;
      }

      MySqlDataReader reader = null;

      try
      {
        MySqlCommand Command = new MySqlCommand();
        Command.CommandText = "select Count(*) as Count from " + TableName;

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
        CacheHandler.AddToCache(ToString(), "CountItems", new string[] { }, CountFromDatabase);
        return CountFromDatabase;
      }
      catch (InvalidCastException)
      {
        reader.Close();
        return 0;
      }
      catch (Exception e)
      {
        if (exceptionHandler(e, "CountItems"))
        {
          return CountItems();
        }
        else
        {
          return 0;
        }
      }
    }

    protected virtual T[] GetItemsByFilter(Filter filter, int LIMIT = 0, OrderBy orderBy = OrderBy.ASC, string OrderByPropertyName = null)
    {
      return GetItemsByFilter(new Filter[] { filter }, LIMIT, orderBy, OrderByPropertyName);
    }

    protected virtual T[] GetItemsByFilter(IEnumerable<Filter> filters, int LIMIT = 0, OrderBy orderBy = OrderBy.ASC, string OrderByPropertyName = null)
    {
      BeforeQuery();

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
        Command.CommandText = "SELECT * FROM " + TableName + " WHERE ";

        foreach (Filter f in filters)
        {
          Command.CommandText += "(" + f.ToString() + ") AND ";

          for (int i = 0; i < f.values.Count; i++)
          {
            Command.Parameters.AddWithValue(f.PropertyNames[i] + i, f.values[i]);
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
          return GetItemsByFilter(filters, LIMIT, orderBy, OrderByPropertyName);
        }
        else
        {
          return null;
        }
      }
    }

    public virtual bool Delete(T Object)
    {
      return Delete(Object.ID);
    }

    public virtual bool Delete(int ID)
    {
      try
      {
        BeforeQuery();

        MySqlCommand Command = new MySqlCommand();
        Command.CommandText = "DELETE FROM " + TableName + " WHERE ID = @ID";
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
          return Delete(ID);
        }
        else
        {
          return false;
        }
      }
    }

    public virtual bool Delete(IEnumerable<int> ObjectIds)
    {
      try
      {
        BeforeQuery();

        if (ObjectIds == null || ObjectIds.Count() == 0)
        {
          return false;
        }

        MySqlCommand Command = new MySqlCommand();

        Command.CommandText = string.Empty;
        StringBuilder LogText = new StringBuilder();

        for (int i = 0; i < ObjectIds.Count(); i++)
        {
          Command.CommandText += string.Format("DELETE FROM " + TableName + " WHERE ID = @ID{0};", i);
          Command.Parameters.AddWithValue("@ID" + i, ObjectIds.ElementAt(i));
          LogText.AppendLine(objectType.Name.ToString() + " " + ObjectIds.ElementAt(i) + " has been deleted.");
        }

        DatabaseHandler.ExecuteNonQuery(Command, LogDatabaseStats);
        CacheHandler.ClearData(ToString());
        EasyMySqlLog.Log(this, LogText.ToString(), logSeverity.Info);

        return true;
      }
      catch (Exception e)
      {
        if (exceptionHandler(e, "DeleteObjects=" + ObjectIds))
        {
          return Delete(ObjectIds);
        }
        else
        {
          return false;
        }
      }
    }

    public virtual bool Delete(IEnumerable<T> Objects)
    {
      if (Objects != null && Objects.Count() > 0)
      {
        return Delete(Objects.Select(o => o.ID).ToArray());
      }

      return false;
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

    private void BeforeQuery()
    {
      if (Settings.ForceRestructure)
      {
        ForceRestructure();
      }
    }

    private void ForceRestructure()
    {
      if (!HasRestructered)
      {
        HasRestructered = true;

        RestructureTable();
      }
    }

    public override string ToString()
    {
      return GetType().Name;
    }

    public void Dispose()
    {

    }
  }
}
