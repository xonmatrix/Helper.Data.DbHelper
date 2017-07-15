﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Data.Common;
using System.Data;
using System.Linq;

namespace Helper.Data
{
    public abstract class DbHelper : IDisposable
    {
        protected bool autoClose = true;
        protected void buildInsertQuery(IDbQuery cmd, string tableName, DbModel data)
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

        public Task Insert(string tableName, DbModel data)
        {
            using (var cmd = this.createQuery())
            {
                buildInsertQuery(cmd, tableName, data);
                return cmd.ExecuteNonQuery();
            }
        }

        protected abstract IDbQuery createQuery();
        public abstract Task<int> InsertWithIdentity(string tableName, DbModel data);

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


        #region Query & Execute 

        public IDbQuery Query(string query, params object[] parameters)
        {
            using (var cmd = this.createQuery())
            {
                return cmd.Append(query, parameters);
            }
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

        #region Connection 


        public abstract void BeginTransaction();

        public abstract void Commit();

        public abstract void Rollback();

        #endregion

        public virtual void Dispose()
        {
        }

    }

    
}
