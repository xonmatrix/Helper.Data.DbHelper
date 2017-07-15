using System;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Data;

namespace Helper.Data
{
    public class SqlHelper : DbHelper, IDbHelper
    {
        private SqlConnection connection;
        private SqlTransaction currTransation;

        public SqlHelper(string connectionString)
        {
            connection = new SqlConnection(connectionString);
        }

        public SqlHelper(SqlConnection conn)
        {
            this.connection = conn;
            autoClose = false;
        }

        public async override Task<int> InsertWithIdentity(string tableName, DbModel data)
        {
            using (var cmd = this.createQuery())
            {
                buildInsertQuery(cmd, tableName, data);
                cmd.Append("SELECT SCOPE_IDENTITY();");
                return await cmd.Value<int>();
            }
        }

        protected override IDbQuery createQuery()
        {
            using (var cmd = this.connection.CreateCommand())
            {
                if (this.currTransation != null)
                    cmd.Transaction = this.currTransation;

                return new SqlQuery(cmd);
            }
        }

        public override void BeginTransaction()
        {
            try
            {
                if (this.connection.State == ConnectionState.Closed)
                    this.connection.Open();

                this.currTransation = this.connection.BeginTransaction();
            }
            catch (SqlException e)
            {
                throw new DbException(e);
            }
        }

        public override void Commit()
        {
            this.currTransation.Commit();
            this.currTransation.Dispose();
            this.currTransation = null;
        }

        public override void Rollback()
        {
            this.currTransation.Rollback();
            this.currTransation.Dispose();
            this.currTransation = null;
        }

        public override void Dispose()
        {
            base.Dispose();
            this.connection.Dispose();
        }
    }
}
