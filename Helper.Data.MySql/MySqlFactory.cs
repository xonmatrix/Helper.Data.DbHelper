using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using Helper.Data.Core.Interfaces;
using MySql.Data.MySqlClient;

namespace Helper.Data.MySqlHelper
{
    public class MySqlFactory : IDbFactory
    {

        public DbConnection CreateConnection(string connectionString)
        {
            return new MySqlConnection(connectionString);
        }


    }
}
