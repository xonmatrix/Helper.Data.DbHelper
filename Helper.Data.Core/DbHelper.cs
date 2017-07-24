using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Data.Common;
using System.Data;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Helper.Data
{
    public class DbHelper : IDisposable
    {
        private DbConnection conn;
        private DbTransaction currTransaction;
        private SqlEngine engine;
        public DbHelper(DbConnection conn, SqlEngine engine = SqlEngine.MySql)
        {
            this.conn = conn;
            this.engine = engine;
        }

        #region Insert, Update, Delete 

        #region Insert 

        public Task Insert(string tableName, IEnumerable<DbModel> data)
        {
            using (var cmd = this.createQuery())
            {
                List<string> values = new List<string>();
                bool isFirst = true;
                cmd.Append("INSERT INTO ").Append(tableName);
                foreach (var d in data)
                {
                    if (isFirst)
                    {
                        cmd.Append(" (");
                        cmd.Append(string.Join(",", d.Data.Keys));
                        cmd.Append(" ) VALUES ");
                    }
                    else
                        cmd.Append(",");

                    cmd.Append(" (");
                    cmd.Append(string.Join(",", d.Data.Values.Select(v => cmd.AppendValue(v))));
                    cmd.Append(" )");
                    isFirst = false;
                }
                cmd.Append(";");
                return cmd.ExecuteNonQuery();
            }
        }

        public Task Insert(string tableName, object data)
        {
            using (var cmd = this.createQuery())
            {
                buildInsertQuery(cmd, tableName, data);
                return cmd.ExecuteNonQuery();
            }
        }

        private void buildInsertQuery(DbQuery cmd, string tableName, object data)
        {
            switch (data)
            {
                case DbModel dbModel:
                    this.buildInsertQuery(cmd, tableName, dbModel);
                    break;
                case JObject jObject:
                    this.buildInsertQuery(cmd, tableName, jObject);
                    break;
                default:
                    this.buildInsertQuery(cmd, tableName, JObject.FromObject(data));
                    break;
            }
        }

        private void buildInsertQuery(DbQuery cmd, string tableName, JObject data)
        {
            List<string> values = new List<string>();
            cmd.Append("INSERT INTO ").Append(tableName).Append(" (");
            bool isFirst = true;
            foreach (var pair in data)
            {
                if (!isFirst)
                    cmd.Append(",");
                cmd.Append(pair.Key);
                switch (pair.Value.Type)
                {
                    case JTokenType.Boolean:
                        values.Add(cmd.AppendValue(pair.Value.Value<bool>()));
                        break;
                    case JTokenType.Bytes:
                        values.Add(cmd.AppendValue(pair.Value.Value<byte[]>()));
                        break;
                    case JTokenType.Date:
                        values.Add(cmd.AppendValue(pair.Value.Value<DateTime>()));
                        break;
                    case JTokenType.Float:
                        values.Add(cmd.AppendValue(pair.Value.Value<float>()));
                        break;
                    case JTokenType.Integer:
                        values.Add(cmd.AppendValue(pair.Value.Value<int>()));
                        break;
                    case JTokenType.String:
                        values.Add(cmd.AppendValue(pair.Value.Value<string>()));
                        break;
                    default:
                        values.Add(cmd.AppendValue(pair.Value));
                        break;
                }

                isFirst = false;
            }
            cmd.Append(") VALUES (").Append(string.Join(",", values)).Append(");");
        }

        private void buildInsertQuery(DbQuery cmd, string tableName, DbModel data)
        {
            List<string> values = new List<string>();
            cmd.Append("INSERT INTO ").Append(tableName).Append(" (");
            bool isFirst = true;
            foreach (var pair in data.Data)
            {
                if (!isFirst)
                    cmd.Append(",");
                cmd.Append(pair.Key);
                values.Add(cmd.AppendValue(pair.Value));
                isFirst = false;
            }
            cmd.Append(") VALUES (").Append(string.Join(",", values)).Append(");");
        }

        public async Task<int> InsertWithIdentity(string tableName, DbModel data)
        {
            using (var cmd = this.createQuery())
            {
                buildInsertQuery(cmd, tableName, data);
                if (engine == SqlEngine.MSSql)
                    cmd.Append("SELECT SCOPE_IDENTITY();");
                else if (engine == SqlEngine.MySql)
                    cmd.Append("SELECT LAST_INSERT_ID();");

                return await cmd.Value<int>();
            }
        }

        #endregion

        public Task Update(string tableName, DbModel data, string condition, params object[] parameters)
        {
            using (var cmd = this.createQuery())
            {
                cmd.Append("UPDATE ").Append(tableName).Append(" SET ");
                bool isFirst = true;
                foreach (var pair in data.Data)
                {
                    if (!isFirst)
                        cmd.Append(",");

                    cmd.Append(pair.Key).Append(" = ").Append(cmd.AppendValue(pair.Value));
                    isFirst = false;
                }
                cmd.Where(condition, parameters);
                cmd.Append(";");
                return cmd.ExecuteNonQuery();
            }
        }

        public Task Delete(string tableName, string condition, params object[] parameters)
        {
            using (var cmd = this.createQuery())
            {
                cmd.Append("DELETE FROM ").Append(tableName);
                cmd.Where(condition, parameters);
                cmd.Append(";");
                return cmd.ExecuteNonQuery();
            }
        }

        #endregion

        #region Query creation & Execute 

        private DbQuery createQuery()
        {
            var cmd = this.conn.CreateCommand();
            if (currTransaction != null)
                cmd.Transaction = this.currTransaction;

            return new DbQuery(cmd);
        }

        public DbQuery Query(string query, params object[] parameters)
        {
            var cmd = this.createQuery();
            return cmd.Append(query, parameters);
        }

        public Task Execute(string query, params object[] parameters)
        {
            using (var cmd = this.createQuery())
            {
                cmd.Append(query, parameters);
                return cmd.ExecuteNonQuery();
            }
        }

        #endregion

        #region Trasacion 

        public void BeginTransaction()
        {

            if (this.conn.State == ConnectionState.Closed)
                this.conn.Open();

            this.currTransaction = this.conn.BeginTransaction();
        }

        public void Commit()
        {
            try
            {
                this.currTransaction.Commit();
            }
            catch (DbException)
            {
                throw;
            }
            finally
            {
                this.currTransaction.Dispose();
                this.currTransaction = null;
            }
        }

        public void Rollback()
        {
            try
            {
                this.currTransaction.Rollback();
            }
            catch (DbException)
            {
                throw;
            }
            finally
            {
                this.currTransaction.Dispose();
                this.currTransaction = null;
            }
        }

        #endregion

        public void Dispose()
        {
            this.conn.Dispose();
            this.conn = null;
            System.Diagnostics.Debug.WriteLine("DbHelper Dispose.");
        }

    }


}
