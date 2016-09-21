using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace EasyMySql.Core
{
    public class AppConfig
    {
        private static AppConfig _Instance = null;
        public static AppConfig Instance
        {
            get
            {
                if (_Instance == null)
                {
                    _Instance = GetAppConfig();
                }

                return _Instance;
            }
        }

        public int CurrentDatabaseConnection { get; set; }
        public bool EnableLogging { get; set; }
        public bool EnalbleMail { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public List<string> DatabaseConnections { get; private set; }

        private AppConfig()
        {
            this.DatabaseConnections = new List<string>();
        }

        public string Serialize()
        {
            return Encrypt.EncryptString(JsonConvert.SerializeObject(this));
        }

        private static AppConfig GetAppConfig()
        {
            string InputString = ConfigurationManager.AppSettings["AppConfig"];

            if (InputString == null || InputString == string.Empty)
            {
                Console.WriteLine("Could not read AppConfig data from AppSettings, Set parameters manually. (AppConfig.Instance)");
                System.Diagnostics.Debug.WriteLine("Could not read AppConfig data from AppSettings, Set parameters manually. (AppConfig.Instance)");
                return new AppConfig();
            }
            else
            {
                return JsonConvert.DeserializeObject<AppConfig>(Encrypt.DecryptString(InputString));
            }
        }

    }
}
