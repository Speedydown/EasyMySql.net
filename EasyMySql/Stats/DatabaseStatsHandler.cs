using EasyMySql.Core;
using EasyMySql.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EasyMySql.Stats
{
    public sealed class DatabaseStatsHandler : DataHandler<DatabaseStats>
    {
        public static readonly DatabaseStatsHandler instance = new DatabaseStatsHandler();

        private DatabaseStatsHandler()
            : base()
        {
            tableName = Constants.InternalTablePrefix + "DatabaseStats";
            LogErrors = false;
        }

        public DatabaseStats GetStatsByDate(string Date)
        {
            DatabaseStats[] DataList = GetItems("Date", Date, true);

            if (DataList.Count() > 0)
            {
                return DataList[0];
            }
            else
            {
                return null;
            }
        }

        public DatabaseStats[] GetDatabaseStats(int NumberOfDays = 30)
        {
            return GetItems(NumberOfDays, OrderBy.DESC, "ID");
        }
    }
}
