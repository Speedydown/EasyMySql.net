using EasyMySql.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyMySql_Test.Model
{
    public class TestHandler : DataHandler<Test>
    {
        public static readonly TestHandler instance = new TestHandler();

        private TestHandler()
        {
            tableName = "test11";
        }

        public Test[] TestFilter()
        {
            Filter filter = new Filter(FilterType.And, null, null);
            filter.AddCondition("IntValue", 339223);

            return GetItemsByFilter(filter);
        }

        public void Restructure()
        {
            base.RestructureTable();
        }
    }
}
