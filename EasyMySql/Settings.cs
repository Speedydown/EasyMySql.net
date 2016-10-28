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
    private static string _ConnectionString;
    /// <summary>
    /// the Mysql Connectionstring.
    /// </summary>
    public static string ConnectionString
    {
      get
      {
        if (string.IsNullOrWhiteSpace(_ConnectionString))
        {
          Console.WriteLine("Could not read ConnectionString data from App.Config, Set ConnectionString manually. (Settings.ConnectionString)");
          System.Diagnostics.Debug.WriteLine("Could not read ConnectionString data from App.Config, Set ConnectionString manually. (Settings.ConnectionString)");
        }

        return _ConnectionString;
      }
      set { _ConnectionString = value; }
    }

    /// <summary>
    /// Enables SQL logging, See EasyMySqlLog class for more info.
    /// </summary>
    public static bool LoggingEnabled { get; set; }

    static Settings()
    {
      LoggingEnabled = false;
    }
  }
}
