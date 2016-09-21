using EasyMySql.Stats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.UI;

namespace EasyMySql.Performance
{
    public class Performance
    {
        private int NumberOfqueries { get; set; }
        private DateTime StartTime { get; set; }

        public Performance()
        {
            this.NumberOfqueries = DatabaseStats.CurrentStats.Requests;
            this.StartTime = TimeConverter.GetDateTime();
        }

        public string GetResults()
        {
            TimeSpan Result = TimeConverter.GetDateTime().Subtract(this.StartTime);
            int TotalNumberofQueries = DatabaseStats.CurrentStats.Requests - this.NumberOfqueries;

            return "This page took " + Result.Milliseconds + " milliseconds to load and executed a total of " + TotalNumberofQueries + " queries.";
        }
    }
}
