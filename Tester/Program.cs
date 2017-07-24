using System;
using Helper.Data;
using MySql.Data.MySqlClient;
using System.Threading.Tasks;

namespace Tester
{
    class Program
    {
        static void Main(string[] args)
        {
            var t = new Task(async () =>
            {
                using (var db = new DbHelper(new MySqlConnection("server=localhost;port=13306;user id=dev;Password=a993cH0lT3cH;database=webtrack2;persist security info=False;Pooling=true;UseCompression=true")))
                {
                    for (int i = 0; i < 100000; i++)
                    {
                        var d = await db.Query("SELECT * FROM user WHERE ID = {0}", 1).ToJObject();
                    }
                }
            });
            t.Start();
            t.Wait();
            Console.WriteLine("Hello World!");
        }
    }
}
