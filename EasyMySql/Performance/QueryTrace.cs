using EasyMySql.Core;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace EasyMySql.Performance
{
    public static class QueryTrace
    {
        private static List<string> ActiveQueries = new List<string>();

        internal static int GetActiveQueryCount
        {
            get
            {
                return ActiveQueries.Count;
            }
        }

        internal static void AddQuery(string Query, MySqlParameterCollection Params)
        {
            ActiveQueries.Add(Query.Trim());

            if (Settings.LoggingEnabled)
            {
                string Output = "Query " + GetActiveQueryCount + ": " + Query + "\n\n";

                foreach (MySqlParameter p in Params)
                {
                    Output += "  [" + p.ParameterName + "][" + p.Value + "]\n";
                }

                System.Diagnostics.Debug.WriteLine(Output);
                Console.WriteLine(Output);
            }
        }

        internal static void RemoveQuery(string Query)
        {
            Query = Query.Trim();
            foreach (string s in ActiveQueries)
            {
                if (s == Query)
                {
                    ActiveQueries.Remove(s);
                    break;
                }
            }
        }

        public static string GetTrace()
        {
            string Output = "Active queryies are: \n";

            foreach (string s in ActiveQueries)
            {
                Output += s + "\n";
            }

            return Output;
        }
    }
}
