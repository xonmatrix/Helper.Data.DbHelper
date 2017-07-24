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
            using (var cmd = this.createCommand())
            using (var query = new DbQuery(cmd))
            {
                List<string> values = new List<string>();
                bool isFirst = true;
                query.Append("INSERT INTO ").Append(tableName);
                foreach (var d in data)
                {
                    if (isFirst)
                    {
                        query.Append(" (");
                        query.Append(string.Join(",", d.Data.Keys));
                        query.Append(" ) VALUES ");
                    }
                    else
                        query.Append(",");

                    query.Append(" (");
                    query.Append(string.Join(",", d.Data.Values.Select(v => query.AppendValue(v))));
                    query.Append(" )");
                    isFirst = false;
                }
                query.Append(";");
                return query.ExecuteNonQuery();
            }
        }

        public Task Insert(string tableName, object data)
        {
            using (var cmd = this.createCommand())
            using (var query = new DbQuery(cmd))
            {
                buildInsertQuery(query, tableName, data);
                return query.ExecuteNonQuery();
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
            using (var cmd = this.createCommand())
            using (var query = new DbQuery(cmd))
            {
                buildInsertQuery(query, tableName, data);
                if (engine == SqlEngine.MSSql)
                    query.Append("SELECT SCOPE_IDENTITY();");
                else if (engine == SqlEngine.MySql)
                    query.Append("SELECT LAST_INSERT_ID();");

                return await query.Value<int>();
            }
        }

        #endregion

        public Task Update(string tableName, DbModel data, string condition, params object[] parameters)
        {
            using (var cmd = this.createCommand())
            using (var query = new DbQuery(cmd))
            {
                query.Append("UPDATE ").Append(tableName).Append(" SET ");
                bool isFirst = true;
                foreach (var pair in data.Data)
                {
                    if (!isFirst)
                        query.Append(",");

                    query.Append(pair.Key).Append(" = ").Append(query.AppendValue(pair.Value));
                    isFirst = false;
                }
                query.Where(condition, parameters);
                query.Append(";");
                return query.ExecuteNonQuery();
            }
        }

        public Task Delete(string tableName, string condition, params object[] parameters)
        {
            using (var cmd = this.createCommand())
            using (var query = new DbQuery(cmd))
            {
                query.Append("DELETE FROM ").Append(tableName);
                query.Where(condition, parameters);
                query.Append(";");
                return query.ExecuteNonQuery();
            }
        }

        #endregion

        #region Query creation & Execute 


        private DbCommand createCommand()
        {
            var cmd = this.conn.CreateCommand();
            if (currTransaction != null)
            {
                cmd.Transaction = this.currTransaction;
            }

            return cmd;
        }

        public DbQuery Query(string queryString, params object[] parameters)
        {
            using (var cmd = this.createCommand())
            {
                return new DbQuery(cmd).Append(queryString, parameters);
            }

        }

        public Task Execute(string queryString, params object[] parameters)
        {
            using (var cmd = this.createCommand())
            using (var query = new DbQuery(cmd))
            {
                query.Append(queryString, parameters);
                return query.ExecuteNonQuery();
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
