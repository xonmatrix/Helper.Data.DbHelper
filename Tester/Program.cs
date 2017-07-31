using System;
using Helper.Data;
using MySql.Data.MySqlClient;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Tester
{
    class Program
    {
        static void Main(string[] args)
        {
            DbModel original = new DbModel();
            original["deviceID"] = "96837524";
            original["description"] = "Car Tracker 8 Fault";
            original["equipmenttype"] = "Pulse GPRS 3";
            original["revereseGeocode"] = 0;
            original["imeiNumber"] = "352544071897582";
            original["isActive"] = 1;
            original["FK_accountID"] = 1;
           

            DbModel bnew = new DbModel();
            bnew["deviceID"] = "96837524";
            bnew["description"] = "Car Tracker 8 Fault";
            bnew["equipmenttype"] = "Pulse GPRS 2";
            bnew["revereseGeocode"] = 0;
            bnew["imeiNumber"] = "352544071897582";
            bnew["isActive"] = 0;
            bnew["nf"] = 1;
            bnew["FK_accountID"] = 2;
            bnew["FK_profileID"] = 2;

            Dictionary<string, string> dictField = new Dictionary<string, string>()
            {
                {"FK_accountID", "AccountName"},
                {"FK_profileID", "ProfileName"},
            };

            var diff = new DbCompare(original, bnew, dictField);
            using (var db = new DbHelper(new MySqlConnection("server=localhost;port=13306;user id=root;Password=echol123tech;database=certisqr;persist security info=False;Pooling=true;UseCompression=true")))
            {
                diff.MapValue("FK_accountID", (val) =>
                {
                    return db.Query("SELECT accountName FROM account WHERE id = {0}", val).Value<string>().Result;
                }).MapValue("FK_profileID", (val) =>
                {
                    return db.Query("SELECT name FROM profile WHERE id = {0}", val).Value<string>().Result;
                }).MapValue("isActive", (val) =>
                {
                    return ((int)val == 1) ? "Active" : "Inactive";
                });
   
            
                var abc = diff.CompareDbModels();
            }

            Console.WriteLine("Hello World!");
        }
    }
}
