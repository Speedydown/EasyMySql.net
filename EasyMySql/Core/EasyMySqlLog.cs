using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;

namespace EasyMySql.Core
{
    internal enum logSeverity { Info = 0, Warning = 1, Error = 2, Critical = 3, Failure = 4 }

    /// <summary>
    /// Handles logging of EasyMySql.
    /// </summary>
    public sealed class EasyMySqlLog
    {
        internal static void Log(object Sender, string Message, logSeverity Severity)
        {
            instance.AddMessage(Sender, Message, Severity);
        }

        public static readonly EasyMySqlLog instance = new EasyMySqlLog();

        private List<string> LogEntries { get; set; }
        public Action<string> LogOutputAction { get; set; }

        public bool LogToConsoleEnabled { get; set; }
        public bool LoggingEnabled { get; set; }
        public bool OnlyLogErrors { get; set; }

        private EasyMySqlLog()
        {
            LogToConsoleEnabled = true;
            LoggingEnabled = true;
            LogEntries = new List<string>();
        }

        internal void AddMessage(object Sender, string Message, logSeverity Severity)
        {
            if (LoggingEnabled && (!OnlyLogErrors ||(OnlyLogErrors == true && Severity > logSeverity.Failure)))
            {
                string FormattedMessage = string.Format("[{0}] - [{1}][{2}]: {3}", Severity.ToString(), TimeConverter.GetDateTime().ToString("d-M-yyyy hh:mm"), Sender.ToString(), Message);

                Debug.WriteLine(FormattedMessage);
                LogEntries.Add(FormattedMessage);

                if (LogToConsoleEnabled)
                {
                    Console.WriteLine(FormattedMessage);
                }

                if (LogOutputAction != null)
                {
                    LogOutputAction.Invoke(FormattedMessage);
                }
            }
        }

        private string[] GetLog()
        {
            return LogEntries.ToArray();
        }    
    }
}