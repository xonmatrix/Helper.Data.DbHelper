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
            int ordinal = r.GetOrdinal(columnName);
            if (r.IsDBNull(ordinal))
                return null;
            else
                return r.GetString(r.GetOrdinal(columnName));
        }

        public static int GetInt(this DbDataReader r, string columnName)
        {
            return r.GetInt32(r.GetOrdinal(columnName));
        }

        public static int GetInt(this DbDataReader r, string columnName, int defaultValue)
        {
            int ordinal = r.GetOrdinal(columnName);
            if (r.IsDBNull(ordinal))
                return defaultValue;
            else
                return r.GetInt32(ordinal);
        }

        public static bool GetBoolean(this DbDataReader r, string columnName)
        {
            return r.GetBoolean(r.GetOrdinal(columnName));
        }

        public static bool GetBoolean(this DbDataReader r, string columnName, bool defaultValue)
        {
            int ordinal = r.GetOrdinal(columnName);
            if (r.IsDBNull(ordinal))
                return defaultValue;
            else
                return r.GetBoolean(ordinal);
        }

        public static DateTime GetDateTime(this DbDataReader r, string columnName)
        {
            return r.GetDateTime(r.GetOrdinal(columnName));
        }

        public static DateTime? GetDateTimeValue(this DbDataReader r, string columnName)
        {
            int ordinal = r.GetOrdinal(columnName);
            if (r.IsDBNull(ordinal))
                return null;
            else
                return r.GetDateTime(ordinal);
        }

        public static JObject GetJObject(this DbDataReader r, string columnName)
        {
            int ordinal = r.GetOrdinal(columnName);
            if (r.IsDBNull(ordinal))
                return null;
            else
                return JObject.Parse(r.GetString(ordinal));
        }

        public static JArray GetJArray(this DbDataReader r, string columnName)
        {
            int ordinal = r.GetOrdinal(columnName);
            if (r.IsDBNull(ordinal))
                return null;
            else
                return JArray.Parse(r.GetString(ordinal));
        }


    }
}
