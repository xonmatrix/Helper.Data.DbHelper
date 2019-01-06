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
            Task.Run(async() =>
            {
                JArray res = new JArray();
                JObject custA = new JObject();
                custA["Name"] = "Cust T";
                custA["Balance"] = 13.00d;
                res.Add(custA);

                JObject custb = new JObject();
                custb["Name"] = "Cust T";
                custb["Balance"] = 13.00d;
                res.Add(custb);

                using(var db = new DbHelper(new MySqlConnection("server=localhost;port=13306;uid=root;password=a993cH0lT3cH;database=ktapp")))
                {
                    await db.Insert("account", res);
                }

            }).GetAwaiter().GetResult();
            //GC.Collect();
            Console.ReadLine();
        }
    }
}
