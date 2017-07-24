using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Threading.Tasks;
using System.Linq;
using System.Data.Common;
using MySql.Data.MySqlClient;

namespace Helper.Data
{
    public class MySqlHelper : DbHelper, IDbHelper
    {
        private MySqlConnection connection;
        private MySqlTransaction currTransaction;

        public MySqlHelper(string connectionString)
        {
            this.connection = new MySqlConnection(connectionString);
        }

        public MySqlHelper(MySqlConnection conn)
        {
            this.connection = conn;
            this.autoClose = false;
        }

        public async override Task<int> InsertWithIdentity(string tableName, DbModel data)
        {
            using (var cmd = this.connection.CreateCommand())
            {
                using (var query = new MySqlQuery(cmd))
                {
                    buildInsertQuery(query, tableName, data);
                    await query.ExecuteNonQuery();
                    return (int)cmd.LastInsertedId;
                }
            }
        }

        protected override IDbQuery createQuery()
        {
            var cmd = this.connection.CreateCommand();
            if (this.currTransaction != null)
                cmd.Transaction = this.currTransaction;

            return new MySqlQuery(cmd);
        }

        public override void BeginTransaction()
        {

            if (this.connection.State == ConnectionState.Closed)
                this.connection.Open();

            this.currTransaction = this.connection.BeginTransaction();
        }

        public override void Commit()
        {
            try
            {
                this.currTransaction.Commit();
            }
            catch (MySqlException e)
            {
                throw;
            }
            finally
            {
                this.currTransaction.Dispose();
                this.currTransaction = null;
            }
        }

        public override void Rollback()
        {
            try
            {
                this.currTransaction.Rollback();
            }
            catch (MySqlException e)
            {
                throw;
            }
            finally
            {
                this.currTransaction.Dispose();
                this.currTransaction = null;
            }
        }

        public override void Dispose()
        {
            System.Diagnostics.Debug.WriteLine("MysqlHelper Dispose.");
            this.connection.Dispose();
            this.connection = null;
            base.Dispose();
        }
    }
}
