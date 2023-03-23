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
        public SqlEngine DbEngine;
        public int CommandTimeout { get; set; } = 30;

        public DbHelper(DbConnection conn, SqlEngine engine = SqlEngine.MySql)
        {
            this.conn = conn;
            this.DbEngine = engine;
        }

        #region Insert, Update, Delete 

        #region Insert 

        public Task Insert(string tableName, IEnumerable<DbModel> data)
        {
            using (var query = new DbQuery(this.createCommand()))
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

        public Task Insert(string tableName, JArray data)
        {
            using (var query = new DbQuery(this.createCommand()))
            {
                List<string> values = new List<string>();
                bool isFirst = true;
                query.Append("INSERT INTO ").Append(tableName);
                foreach (JObject d in data)
                {
                    if (isFirst)
                    {
                        query.Append(" (");
                        query.Append(string.Join(",", d.Properties().Select(a => a.Name)));
                        query.Append(" ) VALUES ");
                    }
                    else
                        query.Append(",");

                    query.Append(" (");
                    query.Append(string.Join(",", d.Properties().Select(a => query.AppendValue(convertJToKenValue(a.Value)))));
                    query.Append(" )");
                    isFirst = false;
                }
                query.Append(";");
                return query.ExecuteNonQuery();
            }
        }

        public Task InsertIgnoreInto(string tableName, IEnumerable<DbModel> data)
        {
            using (var query = new DbQuery(this.createCommand()))
            {
                List<string> values = new List<string>();
                bool isFirst = true;
                query.Append("INSERT IGNORE INTO ").Append(tableName);

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

        public Task Insert(string tableName, JObject data)
        {
            using (var query = new DbQuery(this.createCommand()))
            {
                buildInsertQuery(query, tableName, data);
                return query.ExecuteNonQuery();
            }
        }

        public Task Insert(string tableName, DbModel data)
        {
            using (var query = new DbQuery(this.createCommand()))
            {
                buildInsertQuery(query, tableName, data);
                return query.ExecuteNonQuery();
            }
        }

        private object convertJToKenValue(JToken property)
        {
            switch (property.Type)
            {
                case JTokenType.Boolean:
                    return property.Value<bool>();
                case JTokenType.Bytes:
                    return property.Value<byte[]>();
                case JTokenType.Date:
                    var dt = property.Value<DateTime>();
                    if (dt.Kind == DateTimeKind.Utc)
                        dt = dt.ToLocalTime();
                    return dt;
                case JTokenType.Float:
                    return property.Value<float>();
                case JTokenType.Integer:
                    return property.Value<int>();
                case JTokenType.String:
                    return property.Value<string>();
                default:
                    return property;
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
                values.Add(cmd.AppendValue(convertJToKenValue(pair.Value)));
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

        private void appendQuerySelectIdentity(DbQuery query)
        {
            if (this.DbEngine == SqlEngine.MSSql)
                query.Append("SELECT CAST(SCOPE_IDENTITY() AS INT);");
            else if (this.DbEngine == SqlEngine.MySql)
                query.Append("SELECT LAST_INSERT_ID();");
        }

        public async Task<int> InsertWithIdentity(string tableName, DbModel data) 
        {
            using (var query = new DbQuery(this.createCommand()))
            {
                buildInsertQuery(query, tableName, data);
                appendQuerySelectIdentity(query);
                return await query.Value<int>();
            }
        }

        public async Task<int> InsertWithIdentity(string tableName, JObject data)
        {
            using (var query = new DbQuery(this.createCommand()))
            {
                buildInsertQuery(query, tableName, data);
                appendQuerySelectIdentity(query);
                return await query.Value<int>();
            }
        }

        #endregion

        public Task<int> Update(string tableName, DbModel data, string condition, params object[] parameters)
        {
            using (var query = new DbQuery(this.createCommand()))
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

        public Task<int> Delete(string tableName, string condition, params object[] parameters)
        {

            using (var query = new DbQuery(this.createCommand()))
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
                cmd.Transaction = this.currTransaction;

            cmd.CommandTimeout = this.CommandTimeout;
            return cmd;
        }

        public DbQuery Query(string queryString, params object[] parameters)
        {
            return new DbQuery(this.createCommand()).Append(queryString, parameters);
        }

        public CachedQuery PrepareCacheQuery(string querystring)
        {
            var cq = new CachedQuery(this.createCommand());
            cq.Prepare(querystring);
            return cq;
        }

        public Task Execute(string queryString, params object[] parameters)
        {
            using (var query = new DbQuery(this.createCommand()))
            {
                query.Append(queryString, parameters);
                return query.ExecuteNonQuery();
            }
        }

        #endregion

        #region Transaction 

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
            if (this.conn.State == ConnectionState.Open && this.currTransaction != null)
                this.currTransaction.Rollback();

            this.conn.Dispose();
            this.conn = null;
        }

    }


}
