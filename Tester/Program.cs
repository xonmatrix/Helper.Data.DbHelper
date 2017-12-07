using System;
using Helper.Data;
using MySql.Data.MySqlClient;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace Tester
{
    class Program
    {
        static void Main(string[] args)
        {
            Task.Run(async () =>
            {
                using (DbHelper db = new DbHelper(new MySqlConnection("server=103.15.233.218;port=3306;database=webtas;user=dev;password=a993cH0lT3cH;")))
                {
                    var x =await db.Query("SELECT Id,Name FROM staff").ToDictionary<int, string>();
                    /*
                    var x = db.PrepareCacheQuery("SELECT * FROM STAFF WHERE Id ={0}");
                    for (int j = 0; j < 10000; j++)
                    {
                        var d = await x.Get(j % 9 + 2);
                        Console.WriteLine(d["Name"].ToString());

                        // Console.WriteLine("Line : " + i + " Loop: " + j + " Done");
                    }
                    x.Dispose();
                    */
                }

            }).Wait();
            //GC.Collect();
            Console.ReadLine();
        }
    }
}
