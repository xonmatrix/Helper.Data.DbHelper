using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;
using Newtonsoft.Json.Linq;

namespace Helper.Data
{
    public static class DbDataReaderExtensions
    {
        public static string GetString(this DbDataReader r, string columnName)
        {
            return r.GetString(r.GetOrdinal(columnName));
        }

        public static int GetInt(this DbDataReader r, string columnName)
        {
            return r.GetInt32(r.GetOrdinal(columnName));
        }

        public static bool GetBoolean(this DbDataReader r, string columnName)
        {
            return r.GetBoolean(r.GetOrdinal(columnName));
        }

        public static DateTime GetDateTime(this DbDataReader r, string columnName)
        {
            return r.GetDateTime(r.GetOrdinal(columnName));
        }

        public static JObject GetJObject(this DbDataReader r, string columnName)
        {
            return JObject.Parse(r.GetString(columnName));
        }

        public static JArray GetJArray(this DbDataReader r,string columnName)
        {
            return JArray.Parse(r.GetString(columnName));
        }


    }
}
