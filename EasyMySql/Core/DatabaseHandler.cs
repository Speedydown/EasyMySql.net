using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MySql.Data.MySqlClient;
using System.Data;
using System.Configuration;
using System.Threading;
using EasyMySql.Performance;
using EasyMySql.Stats;

namespace EasyMySql.Core
{
    internal static class DatabaseHandler
    {
        private static readonly Semaphore semaphore = new Semaphore(Settings.MaxNumberOfConnections, Settings.MaxNumberOfConnections);

        private static MySqlConnection OpenConnection(bool LogStats)
        {
            MySqlConnection Connection = null;

            try
            {
                Connection = new MySqlConnection(Settings.ConnectionString);
            }
            catch
            {
                throw new InvalidOperationException("No connection string");
            }

            if (!semaphore.WaitOne(10000))
            {
                new EasyMySqlException("DatabaseHandler", new Exception("Maximum connections reached \n" + QueryTrace.GetTrace()));
                return Connection;
            }

            try
            {
                Connection.Open();

                if (LogStats)
                {
                    try
                    {
                        DatabaseStats.CurrentStats.AddDatabaseHit();
                    }
                    catch (Exception)
                    {

                    }
                }
            }
            catch (Exception e)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine("Database connection Failed\n" + e.ToString());
                Console.WriteLine("Database connection Failed\n" + e.ToString());
#else
                throw new CouldNotConnectException("Could not open the connection to the SQL server, Connectionstring correct? /n" + e.ToString());
#endif
            }

            return Connection;
        }

        /// <summary>
        /// Executes a Query that does not return results
        /// </summary>
        /// <param name="Command">MySQLCommand</param>
        internal static void ExecuteNonQuery(MySqlCommand Command, bool LogStats, int RetryCount = 0)
        {
            MySqlConnection Connection = null;

            try
            {
                Connection = OpenConnection(LogStats);
                Command.Connection = Connection;
                QueryTrace.AddQuery(Command.CommandText, Command.Parameters);
                Command.ExecuteNonQuery();
            }
            catch (InvalidOperationException e)
            {
                QueryTrace.RemoveQuery(Command.CommandText);
                if (HandleException(Connection, e, RetryCount))
                {
                    ExecuteNonQuery(Command, LogStats, ++RetryCount);
                }
                else
                {
                    throw e;
                }             
            }
            catch (MySqlException e)
            {
                if (e.Message.StartsWith("Duplicate entry"))
                {
                    throw e;
                }

                QueryTrace.RemoveQuery(Command.CommandText);
                if (HandleException(Connection, e, RetryCount))
                {
                    ExecuteNonQuery(Command, LogStats, ++RetryCount);
                }
                else
                {
                    throw e;
                }             
            }
            catch (CouldNotConnectException e)
            {
                throw new Exception("Database connection not valid. \n" + e.ToString());
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                CloseConnection(Connection);
                QueryTrace.RemoveQuery(Command.CommandText);
            }
        }

        /// <summary>
        /// Executes a query the returns a datareader
        /// </summary>
        /// <param name="Command">MysqlCommand, Add params and query before using this method.</param>
        /// <returns>MysqlDataReader</returns>
        internal static MySqlDataReader ExecuteQuery(MySqlCommand Command, bool LogStats, int RetryCount = 0)
        {
            MySqlConnection Connection = null;
            MySqlDataReader Reader = null;

            try
            {
                Connection = OpenConnection(LogStats);
                Command.Connection = Connection;
                QueryTrace.AddQuery(Command.CommandText, Command.Parameters);
                Reader = Command.ExecuteReader(CommandBehavior.CloseConnection);
            }
            catch (InvalidOperationException e)
            {
                if (HandleException(Connection, e, RetryCount))
                {
                    return ExecuteQuery(Command, LogStats, ++RetryCount);
                }
                else
                {
                    throw e;
                }
            }
            catch (CouldNotConnectException e)
            {
                throw new CouldNotConnectException("Database connection not valid. \n" + e.ToString());
            }
            catch (MySqlException e)
            {
                if (HandleException(Connection, e, RetryCount))
                {
                    return ExecuteQuery(Command, LogStats, ++RetryCount);
                }
                else
                {
                    throw e;
                }
            }
            catch (Exception e)
            {
                throw e;
            }

            return Reader;
        }

        private static void CloseConnection(MySqlConnection Connection)
        {
            if (Connection != null)
            {
                if (Connection.State == ConnectionState.Open)
                {
                    try
                    {
                        semaphore.Release();
                    }
                    catch
                    {

                    }

                    try
                    {
                        Connection.Close();
                    }
                    catch
                    {

                    }
                }
            }
        }

        internal static void CloseConnectionByReader(MySqlDataReader Reader)
        {
            Reader.Close();
            semaphore.Release();
        }

        private static bool HandleException(MySqlConnection Connection, Exception e, int RetryCount)
        {
            try
            {
                CloseConnection(Connection);
            }
            catch
            {

            }

            if (e.Message.Contains(" doesn't exist") || e.Message.Contains("Could not find specified column in results") || e.Message.StartsWith("Unknown column") || e.Message.Contains("Duplicate column name") || e.Message.Contains("Multiple primary key defined"))
            {
                return false;
            }

            if (RetryCount > 7)
            {
                throw new CouldNotConnectException(e.ToString());
            }
            else
            {
                return true;
            }
        }
    }

    class CouldNotConnectException : Exception
    {
        public CouldNotConnectException(string message)
            : base(message)
        {
        }
    }
}