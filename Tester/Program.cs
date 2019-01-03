using System;
using Helper.Data;
using MySql.Data.MySqlClient;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Tester
{
    class Program
    {
        static void Main(string[] args)
        {
            Task.Run(() =>
            {
                JObject x = new JObject();
                var arr = new JArray();
                arr.Add("123");
                arr.Add("456");
                arr.Add("789");
                x["Arr"] = arr;

                DbModel cont = x.ToObject<DbModel>();
                var w = cont.List("Arr");
            }).Wait();
            //GC.Collect();
            Console.ReadLine();
        }
    }
}
