using EasyMySql;
using EasyMySql.Core;
using EasyMySql_Test.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyMySql_Test
{
    class Program
    {
        static void Main(string[] args)
        {
            Settings.ConnectionString = "Server=MYSQL5017.SmarterASP.NET;Database=db_9b4757_easysql;Uid=9b4757_easysql;Pwd=Geheim1!";
            //  TestHandler.instance.Restructure();

            int Count = TestHandler.instance.CountItems();

            var filteritems = TestHandler.instance.TestFilter();
//            TestHandler.instance.Add(new Test() { BoolValue = true, DateTimeValue = DateTime.Now, DoubleValue = 0.123, IgnoredValue = true, IntValue = 339223, StringValue = "Teststring" });
         //   TestHandler.instance.AddObject(new Test() { BoolValue = true, DateTimeValue = DateTime.Now, DoubleValue = 0.1, IgnoredValue = true, IntValue = 32, StringValue = "Teststring" });

       //  TestHandler.instance.Save()

            var Result = TestHandler.instance.GetItems();

            Console.ReadLine();
        }
    }
}
