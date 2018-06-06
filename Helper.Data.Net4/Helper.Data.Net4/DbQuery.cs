using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Linq;
using System.Data.Common;

namespace Helper.Data.Net4
{
    public class DbQuery : IDisposable
    {
        private DbCommand command;
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
                string key = $"@Va{valueCount++}";
                switch (value)
                {
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

        public void ExecuteReader(Func<DbDataReader, bool> rowExecution)
        {
            command.CommandText = CommandText.ToString();
            command.CommandType = CommandType.Text;

            if (command.Connection.State != ConnectionState.Open)
                command.Connection.Open();


            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    if (!rowExecution(reader))
                        break;
                }
            }
            command.Dispose();
        }

        public void ExecuteNonQuery()
        {
            command.CommandText = CommandText.ToString();
            command.CommandType = CommandType.Text;

            if (command.Connection.State != ConnectionState.Open)
                command.Connection.Open();

            command.ExecuteNonQuery();
            command.Dispose();
        }

        #endregion

        #region SingleOrDefault , Value , Any

        public T SingleOrDefault<T>(Func<DbDataReader, T> map)
        {
            T result = default(T);
            this.ExecuteReader((reader) =>
            {
                result = map(reader);
                return false;
            });
            return result;
        }

        public T SingleOrDefault<T>()
        {

            return this.SingleOrDefault<T>(reader => mapFieldValue<T>(reader, 0));
        }

        public DbModel SingleOrDefault()
        {
            return this.SingleOrDefault<DbModel>(mapToDbModel);
        }

        public T Value<T>()
        {
            return this.SingleOrDefault(r => mapFieldValue<T>(r, 0));
        }

        public bool Any()
        {
            bool result = false;
            this.ExecuteReader((r) =>
            {
                result = true;
                return false;
            });
            return result;
        }

        #endregion

        #region ToList, Select 

        public List<T> Select<T>(Func<DbDataReader, T> rowReader)
        {

            List<T> results = new List<T>();
            this.ExecuteReader(reader =>
            {
                T row = rowReader(reader);
                if (row != null)
                    results.Add(row);

                return true;
            });
            return results;
        }

        public List<DbModel> ToList()
        {
            return this.Select<DbModel>(mapToDbModel);
        }

        public List<T> ToList<T>()
        {
            return this.Select<T>(r => mapFieldValue<T>(r, 0));
        }

        public Dictionary<T1, T2> ToDictionary<T1, T2>()
        {
            Dictionary<T1, T2> results = new Dictionary<T1, T2>();
            this.ExecuteReader((r) =>
            {
                results.Add(r.Get<T1>(r.GetName(0)), r.Get<T2>(r.GetName(1)));
                return true;
            });
            return results;
        }

        #endregion

     

     

        #region Mapper

        private T mapFieldValue<T>(DbDataReader reader, int index)
        {
            if (reader.IsDBNull(index))
                return default(T);

         

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

       
        #endregion

        public void Dispose()
        {
            this.CommandText = null;
            this.command = null;
        }
    }
}
