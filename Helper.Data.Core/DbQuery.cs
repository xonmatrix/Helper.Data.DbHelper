﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Data.Common;

namespace Helper.Data
{
    public class DbQuery : IDisposable
    {
        private DbCommand command;
        private List<string> jsonFields;

        internal DbQuery(DbCommand cmd)
        {
            this.command = cmd;
        }

        #region Command Builder 

        protected StringBuilder CommandText { get; private set; } = new StringBuilder();
        private int conditionCount = 0;
        private int valueCount = 0;

        public DbQuery Append(string condition)
        {
            this.CommandText.Append(condition);
            return this;
        }

        public DbQuery Append(string condition, params object[] parameters)
        {
            if (parameters != null && parameters.Length > 0)
            {
                string[] conditions = parameters.Select(c => this.AppendCondition(c)).ToArray();
                this.CommandText.Append(string.Format(condition, conditions.ToArray()));
            }
            else
                this.CommandText.Append(condition);

            return this;
        }

        public DbQuery AppendColIn(string colName, params object[] parameters)
        {
            return this.Append($" {colName} IN ({string.Join(",", parameters.Select(p => this.AppendCondition(p)))})");
        }

        private void appendCommand(string key, object value)
        {
            var parameter = this.command.CreateParameter();
            parameter.ParameterName = key;
            parameter.Value = value;
            this.command.Parameters.Add(parameter);
        }

        public string AppendCondition(object value)
        {
            string key = $"@Cond{conditionCount++}";
            this.appendCommand(key, value);
            return key;
        }

        public string AppendValue(object value)
        {
            if (value is DbExpression t)
            {
                return t.Expression;
            }
            else
            {
                string key = $"@VAL_{valueCount++}";
                switch (value)
                {
                    case JObject jo:
                        this.appendCommand(key, jo.ToString());
                        break;
                    case JArray ja:
                        this.appendCommand(key, ja.ToString());
                        break;
                    case null:
                        this.appendCommand(key, DBNull.Value);
                        break;
                    default:
                        this.appendCommand(key, value);
                        break;
                }

                return key;

            }
        }

        public DbQuery Where(string query, params object[] parameters)
        {
            this.Append(" WHERE ");
            this.Append(query, parameters);
            return this;
        }

        public DbQuery And(string query, params object[] parameters)
        {
            return this.Append(" AND ")
                       .Append(query, parameters);

        }

        public DbQuery AndColIn(string colName, params object[] parameters)
        {
            return this.Append(" AND ")
                .AppendColIn(colName, parameters);
        }

        #endregion

        #region Execution

        public async Task ExecuteReader(Func<DbDataReader, bool> rowExecution)
        {
            command.CommandText = CommandText.ToString();
            command.CommandType = CommandType.Text;

            if (command.Connection.State != ConnectionState.Open)
                command.Connection.Open();

            using (var reader = await command.ExecuteReaderAsync())
            {
                while (reader.Read())
                {
                    if (!rowExecution(reader))
                        break;
                }
            }
            command.Dispose();
        }

        public async Task<int> ExecuteNonQuery()
        {
            command.CommandText = CommandText.ToString();
            command.CommandType = CommandType.Text;

            if (command.Connection.State != ConnectionState.Open)
                command.Connection.Open();

            int res = await command.ExecuteNonQueryAsync();
            if (command != null)
                command.Dispose();
            return res;
        }

        #endregion

        #region SingleOrDefault , Value , Any

        public async Task<T> SingleOrDefault<T>(Func<DbDataReader, T> map)
        {
            T result = default(T);
            await this.ExecuteReader((reader) =>
            {
                result = map(reader);
                return false;
            });
            return result;
        }

        public async Task<T> SingleOrDefault<T>()
        {
            if (typeof(T) == typeof(JObject))
            {
                return (T)(object)await this.SingleOrDefault(mapToJObject);
            }
            else
                return await this.SingleOrDefault<T>(reader => mapFieldValue<T>(reader, 0));
        }

        public Task<DbModel> SingleOrDefault()
        {
            return this.SingleOrDefault<DbModel>(mapToDbModel);
        }

        public Task<T> Value<T>()
        {
            return this.SingleOrDefault(r => mapFieldValue<T>(r, 0));
        }

        public async Task<bool> Any()
        {
            bool result = false;
            await this.ExecuteReader((r) =>
            {
                result = true;
                return false;
            }).ConfigureAwait(false);
            return result;
        }

        #endregion

        #region ToList, Select 

        public async Task<List<T>> Select<T>(Func<DbDataReader, T> rowReader)
        {

            List<T> results = new List<T>();
            await this.ExecuteReader(reader =>
            {
                T row = rowReader(reader);
                if (row != null)
                    results.Add(row);

                return true;
            }).ConfigureAwait(false);
            return results;
        }

        public Task<List<DbModel>> ToList()
        {
            return this.Select<DbModel>(mapToDbModel);
        }

        public Task<List<T>> ToList<T>()
        {
            return this.Select<T>(r => mapFieldValue<T>(r, 0));
        }

        public async Task<Dictionary<T1, T2>> ToDictionary<T1, T2>()
        {
            Dictionary<T1, T2> results = new Dictionary<T1, T2>();
            await this.ExecuteReader((r) =>
            {
                results.Add(r.Get<T1>(r.GetName(0)), r.Get<T2>(r.GetName(1)));
                return true;
            });
            return results;
        }

        #endregion

        #region Jobject, Jarray

        public Task<JObject> ToJObject()
        {
            return this.SingleOrDefault(mapToJObject);
        }

        public Task<JArray> ToJArray()
        {
            return ToJArray(mapToJObject);
        }

        public async Task<JArray> ToJArray(Func<DbDataReader, JToken> map)
        {
            JArray results = new JArray();
            await this.ExecuteReader((r) =>
            {
                var obj = map(r);
                if (obj != null)
                    results.Add(obj);
                return true;
            }).ConfigureAwait(false);

            return results;
        }

        #endregion

        #region Transformation

        public DbQuery WithJsonField(params string[] fieldName)
        {
            if (jsonFields == null)
                jsonFields = new List<string>();

            jsonFields.AddRange(fieldName.Select(a => a.ToLower()));
            return this;
        }

        #endregion

        #region Mapper

        private T mapFieldValue<T>(DbDataReader reader, int index)
        {
            if (reader.IsDBNull(index))
                return default(T);

            if (isJsonField(reader.GetName(index)) || typeof(T) == typeof(JToken))
                return (T)(object)JToken.Parse(reader.GetString(index));

            switch (Type.GetTypeCode(typeof(T)))
            {
                case TypeCode.Int32:
                    return (T)(object)reader.GetInt32(index);
                case TypeCode.String:
                    return (T)(object)reader.GetValue(index);
                case TypeCode.Object:
                    return (T)reader.GetValue(index);
                default:
                    return (T)reader.GetValue(index);
            }
        }

        private DbModel mapToDbModel(DbDataReader reader)
        {
            DbModel result = new DbModel();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (reader.IsDBNull(i))
                    result[reader.GetName(i)] = null;
                else if (isJsonField(reader.GetName(i)))
                    result[reader.GetName(i)] = JValue.Parse(reader.GetString(i));
                else
                {
                    var value = reader[i];
                    if (value is DateTime)
                        result[reader.GetName(i)] = DateTime.SpecifyKind(reader.GetDateTime(i), DateTimeKind.Local);
                    else
                        result[reader.GetName(i)] = reader[i];
                }
            }
            return result;
        }

        private JObject mapToJObject(DbDataReader reader)
        {
            JObject row = new JObject();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (reader.IsDBNull(i))
                    row[reader.GetName(i)] = JValue.CreateNull();
                else if (isJsonField(reader.GetName(i)))
                    row[reader.GetName(i)] = JValue.Parse(reader.GetString(i));
                else
                {
                    var value = reader.GetValue(i);
                    //  if (value is DateTime)
                    //    row[reader.GetName(i)] = DateTime.SpecifyKind(reader.GetDateTime(i), DateTimeKind.Local);
                    //  else
                    row[reader.GetName(i)] = new JValue(value);

                }
            }
            return row;
        }

        private bool isJsonField(string fieldName)
        {
            if (this.jsonFields == null)
                return false;
            else return this.jsonFields.Contains(fieldName.ToLower());
        }

        #endregion

        public void Dispose()
        {
            this.CommandText = null;
            this.command = null;
        }
    }
}
