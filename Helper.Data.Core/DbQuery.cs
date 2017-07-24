using System;
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
        public DbQuery(DbCommand cmd)
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
        
        private void appendCommand(string key,object value)
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
                string key = $"@Va{valueCount++}";
                switch (value)
                {
                    case JObject jo:
                        this.appendCommand(key, jo.ToString());
                        break;
                    case JArray ja:
                        this.appendCommand(key, ja.ToString());
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
            this.Append(" AND ");
            this.Append(query, parameters);
            return this;
        }

        #endregion

        #region Execution

        public async Task ExecuteReader(Func<IDataReader, bool> rowExecution)
        {
            command.CommandText = CommandText.ToString();
            command.CommandType = CommandType.Text;

            if (command.Connection.State != ConnectionState.Open)
                command.Connection.Open();


            using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
            {
                while (reader.Read())
                {
                    if (!rowExecution(reader))
                        break;
                }
            }

        }

        public Task ExecuteNonQuery()
        {

            command.CommandText = CommandText.ToString();
            command.CommandType = CommandType.Text;

            if (command.Connection.State != ConnectionState.Open)
                command.Connection.Open();

            return command.ExecuteNonQueryAsync();
        }


        #endregion

        #region SingleOrDefault , Value , Any

        public async Task<T> SingleOrDefault<T>(Func<IDataReader, T> map)
        {
            T result = default(T);
            await this.ExecuteReader((reader) =>
            {
                result = map(reader);
                return false;
            });
            return result;
        }

        public Task<T> SingleOrDefault<T>()
        {
            return this.SingleOrDefault<T>(reader => mapFieldValue<T>(reader, 0));
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

        public async Task<List<T>> Select<T>(Func<IDataReader, T> rowReader)
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

        #endregion

        #region Jobject, Jarray

        public Task<JObject> ToJObject()
        {
            return this.SingleOrDefault(mapToJObject);
        }

        public async Task<JArray> ToJArray()
        {
            JArray results = new JArray();
            await this.ExecuteReader((r) =>
            {
                results.Add(mapToJObject(r));
                return true;
            }).ConfigureAwait(false);

            return results;
        }

        #endregion

        #region Mapper

        private T mapFieldValue<T>(IDataReader reader, int index)
        {
            if (reader.IsDBNull(index))
                return default(T);

            switch (Type.GetTypeCode(typeof(T)))
            {
                case TypeCode.Int32:
                    return (T)(object)reader.GetValue(index);
                case TypeCode.String:
                    return (T)(object)reader.GetValue(index);
                default:
                    return (T)reader.GetValue(index);
            }
        }

        private DbModel mapToDbModel(IDataReader reader)
        {
            DbModel result = new DbModel();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (reader.IsDBNull(i))
                    result[reader.GetName(i)] = null;
                else
                    result[reader.GetName(i)] = reader[i];
            }
            return result;
        }

        private JObject mapToJObject(IDataReader reader)
        {
            JObject row = new JObject();
            for (int i = 0; i < reader.FieldCount; i++)
            {
         
                if (reader.IsDBNull(i))
                    row[reader.GetName(i)] = JValue.CreateNull();
                else
                    row[reader.GetName(i)] = new JValue(reader.GetValue(i));
            }
            return row;
        }

        #endregion

        public virtual void Dispose()
        {
            this.command.Dispose();
            this.command = null;
            System.Diagnostics.Debug.WriteLine("Query dispose");
        }

    }
}
