using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;


namespace Helper.Data.Net4
{
    public static class DbDataReaderExtensions
    {
    
        public static T Get<T>(this DbDataReader r, string columnName)
        {
            if (typeof(T) == typeof(string))
                return (T)(object)GetString(r, columnName);
            else if (typeof(T) == typeof(int))
                return (T)(object)GetInt(r, columnName);
            else if (typeof(T) == typeof(bool))
                return (T)(object)GetBoolean(r, columnName);
            else if (typeof(T) == typeof(double))
                return (T)(object)GetDouble(r, columnName);
            else
                return (T)r.GetValue(r.GetOrdinal(columnName));
        }

        public static double GetDouble(this DbDataReader r, string columnName, double defaultValue = 0d)
        {
            int ordinal = r.GetOrdinal(columnName);
            if (r.IsDBNull(ordinal))
                return defaultValue;
            else if (r.GetFieldType(ordinal) == typeof(decimal))
                return (double)r.GetDecimal(ordinal);
            else
                return r.GetDouble(ordinal);

        }

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

      
    }
}
