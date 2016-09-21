using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EasyMySql
{
    public static class TimeConverter
    {
        public static DateTime GetDateTime()
        {
            try
            {
                return TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "W. Europe Standard Time");
            }
            catch
            {
                return DateTime.Now;
            }
        }

        public static string GetDateTimeAsString()
        {
            return GetDateTime().ToString("d-M-yyyy HH:mm:ss");
        }

    }
}
