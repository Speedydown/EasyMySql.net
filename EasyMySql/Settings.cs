using EasyMySql.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace EasyMySql
{
    /// <summary>
    /// Contains basic settings for EasyMySql.Net.
    /// </summary>
    public static class Settings
    {
        /// <summary>
        /// Sets the maximum number of mysql connections.
        /// </summary>
        public static int MaxNumberOfConnections { internal get; set; }

        private static string _ConnectionString;
        /// <summary>
        /// the Mysql Connectionstring.
        /// </summary>
        public static string ConnectionString
        {
            internal get
            {
                if (string.IsNullOrWhiteSpace(_ConnectionString))
                {
                    string Error = "Could not read ConnectionString data from Settings, Set ConnectionString manually. (Settings.ConnectionString)";
                    Console.WriteLine(Error);
                    System.Diagnostics.Debug.WriteLine(Error);
                    throw new InvalidOperationException(Error);
                }

                return _ConnectionString;
            }
            set { _ConnectionString = value; }
        }

        /// <summary>
        /// Forces a restructure of the MySqlTable when initializing a datahandler.
        /// </summary>
        public static bool ForceRestructure { get; set; }

        /// <summary>
        /// Enables SQL logging, See EasyMySqlLog class for more info.
        /// </summary>
        public static bool LoggingEnabled { get; set; }

        static Settings()
        {
            MaxNumberOfConnections = 10;
            LoggingEnabled = false;
            ForceRestructure = false;
        }

       public static void Init(string ConnectionString, int MaxNumberOfConnections)
        {
            Settings.ConnectionString = ConnectionString;
            Settings.MaxNumberOfConnections = MaxNumberOfConnections;
        }
    }
}
