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
            DbModel row = new DbModel();
            row["Name1"] = "asdfasdfsdf";
            row["Name2"] = "asdfasdfsdfas";
            row["Name3"] = "dfsdfsdfasd";
            row["Name4"] = "fasfasdfa";
            row["Name5"] = "sdfsdfasdfsdfsdfsdfsdf";

            Parallel.For(0, 50  ,async (i) =>
            {
                using (DbHelper db = new DbHelper(new MySqlConnection("server=192.168.0.93;port=13306;database=tests;user=dev;password=a993cH0lT3cH;"))) {
                    for (int j = 0; j < 100000; j++)
                    {

                        await db.Insert("test1", row);
                        Console.WriteLine("Line : " + i +" Loop: " + j + " Done");
                    }
                }
            });

            
        }
    }
}
