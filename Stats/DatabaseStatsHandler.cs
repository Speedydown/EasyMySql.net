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

        private static Field RequestsField = new Field("Requests", typeof(int), 1);
        private static Field DateField = new Field("Date", typeof(string), 25);

        private DatabaseStatsHandler()
            : base(Constants.InternalTablePrefix + "DatabaseStats", new Field[] { RequestsField, DateField })
        {
            RegisterError = false;
        }

        public DatabaseStats GetStatsByDate(string Date)
        {
            DatabaseStats[] DataList = base.GetObjectByFieldsAndSearchQuery(new Field[] { DateField }, Date, true);

            if (DataList.Count() > 0)
            {
                return DataList[0];
            }
            else
            {
                return null;
            }
        }

        public DatabaseStats[] GetLast30DatabaseStats()
        {
            return GetObjectList(30, OrderBy.DESC, new Field("ID", typeof(int), 1));
        }
    }
}
