using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Data.Common;
using System.Data;
using System.Linq;


namespace Helper.Data.Net4
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

        public void Insert(string tableName, IEnumerable<DbModel> data)
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
                query.ExecuteNonQuery();
            }
        }

        public void InsertIgnoreInto(string tableName, IEnumerable<DbModel> data)
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
                query.ExecuteNonQuery();
            }
        }

    

        public void Insert(string tableName, DbModel data)
        {
            using (var query = new DbQuery(this.createCommand()))
            {
                buildInsertQuery(query, tableName, data);
                query.ExecuteNonQuery();
            }
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

        public int InsertWithIdentity(string tableName, DbModel data)
        {
            using (var query = new DbQuery(this.createCommand()))
            {
                buildInsertQuery(query, tableName, data);
                if (this.DbEngine == SqlEngine.MSSql)
                    query.Append("SELECT SCOPE_IDENTITY();");
                else if (this.DbEngine == SqlEngine.MySql)
                    query.Append("SELECT LAST_INSERT_ID();");

                return query.Value<int>();
            }
        }

        #endregion

        public void Update(string tableName, DbModel data, string condition, params object[] parameters)
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
                query.ExecuteNonQuery();
            }
        }

        public void Delete(string tableName, string condition, params object[] parameters)
        {

            using (var query = new DbQuery(this.createCommand()))
            {
                query.Append("DELETE FROM ").Append(tableName);
                query.Where(condition, parameters);
                query.Append(";");
                query.ExecuteNonQuery();
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

        public void Execute(string queryString, params object[] parameters)
        {
            using (var query = new DbQuery(this.createCommand()))
            {
                query.Append(queryString, parameters);
                query.ExecuteNonQuery();
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
            if (this.conn.State == ConnectionState.Open && this.currTransaction != null)
                this.currTransaction.Rollback();

            this.conn.Dispose();
            this.conn = null;
        }

    }


}
