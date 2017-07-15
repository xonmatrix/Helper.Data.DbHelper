using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace Helper.Data
{
    public class MySqlQuery : DbQuery, IDbQuery
    {
        private MySqlCommand cmd;
        public MySqlQuery(MySqlCommand cmd)
        {
            this.cmd = cmd;
        }

        public override void AddCommandParameter(string key, object value)
        {
            cmd.Parameters.AddWithValue(key, value);
        }

        public async override Task ExecuteNonQuery()
        {
            try
            {
                cmd.CommandText = CommandText.ToString();
                cmd.CommandType = CommandType.Text;

                if (cmd.Connection.State != ConnectionState.Open)
                    cmd.Connection.Open();

                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
            catch (MySqlException ex)
            {

                //AUTO rool back
                throw new DbException(ex);
            }
        }

        public async override Task ExecuteReader(Func<IDataReader, bool> rowExecution)
        {
            cmd.CommandText = CommandText.ToString();
            cmd.CommandType = CommandType.Text;

            try
            {
                if (cmd.Connection.State != ConnectionState.Open)
                    cmd.Connection.Open();


                using (var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    while (reader.Read())
                    {
                        if (!rowExecution(reader))
                            break;
                    }
                }
            }
            catch (MySqlException ex)
            {
                throw new DbException(ex);
            }
        }

        public override void Dispose()
        {
            this.cmd = null;
            base.Dispose();
        }
    }
}
