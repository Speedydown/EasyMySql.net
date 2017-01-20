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
            tableName = "test6";
        }

        public void Restructure()
        {
            base.RestructureTable();
        }
    }
}
