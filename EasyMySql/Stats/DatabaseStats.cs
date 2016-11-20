using EasyMySql.Attributes;
using EasyMySql.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EasyMySql.Stats
{
    public sealed class DatabaseStats : DataObject
    {
        private static DatabaseStats _CurrentStats = null;
        public static DatabaseStats CurrentStats
        {
            get
            {
                if (_CurrentStats == null)
                {
                    _CurrentStats = new DatabaseStats(0, 0, TimeConverter.GetDateTime().ToString("d-M-yyyy"));
                    DatabaseStats RetrievedStats = DatabaseStatsHandler.instance.GetStatsByDate(TimeConverter.GetDateTime().ToString("d-M-yyyy"));

                    if (RetrievedStats != null)
                    {
                        _CurrentStats = RetrievedStats;
                    }
                    else
                    {
                        DatabaseStatsHandler.instance.AddObject(_CurrentStats);
                    }
                }

                if (_CurrentStats.Date != TimeConverter.GetDateTime().ToString("d-M-yyyy"))
                {
                    DatabaseStats DatabaseStats = _CurrentStats;
                    _CurrentStats = new DatabaseStats(0, 0, TimeConverter.GetDateTime().ToString("d-M-yyyy"));
                    DatabaseStatsHandler.instance.UpdateObject(DatabaseStats);
                    DatabaseStatsHandler.instance.AddObject(_CurrentStats);
                }

                if (TimeConverter.GetDateTime().Subtract(_CurrentStats.LastUpdated).Minutes > 15)
                {
                    _CurrentStats.SetlastUpdated();
                    DatabaseStatsHandler.instance.UpdateObject(_CurrentStats);
                }

                return _CurrentStats;
            }
        }

        public int Requests { get; private set; }
        [StringLength(Length = 75)]
        public string Date { get; private set; }
        private DateTime LastUpdated { get; set; }

        public DatabaseStats()
        {
            SetlastUpdated();
        }

        public DatabaseStats(int ID, int Requests, string Date)
        {
            this.ID = ID;
            this.Requests = Requests;
            this.Date = Date;
            SetlastUpdated();
        }

        private void SetlastUpdated()
        {
            LastUpdated = TimeConverter.GetDateTime();
        }

        internal int AddDatabaseHit()
        {
            Requests++;

            return Requests;
        }
    }
}
