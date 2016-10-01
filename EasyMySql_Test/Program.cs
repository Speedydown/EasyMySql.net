﻿using EasyMySql.Config;
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
            AppConfig.Instance.DatabaseConnections.Add("Server=MYSQL5017.SmarterASP.NET;Database=db_9b4757_easysql;Uid=9b4757_easysql;Pwd=Geheim1!");
         //   TestHandler.instance.AddObject(new Test() { BoolValue = true, DateTimeValue = DateTime.Now, DoubleValue = 0.1, IgnoredValue = true, IntValue = 32, StringValue = "Teststring" });
            var Result = TestHandler.instance.GetObjectList();

            Console.ReadLine();
        }
    }
}