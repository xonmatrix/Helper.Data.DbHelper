using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;

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

            cmd.CommandText = CommandText.ToString();
            cmd.CommandType = CommandType.Text;

            if (cmd.Connection.State != ConnectionState.Open)
                cmd.Connection.Open();

            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
        }

        public async override Task ExecuteReader(Func<IDataReader, bool> rowExecution)
        {
            cmd.CommandText = CommandText.ToString();
            cmd.CommandType = CommandType.Text;

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

        public override void Dispose()
        {
            if (this.cmd != null)
            {
                this.cmd.Dispose();
                this.cmd = null;
            }
            base.Dispose();
        }
    }
}
