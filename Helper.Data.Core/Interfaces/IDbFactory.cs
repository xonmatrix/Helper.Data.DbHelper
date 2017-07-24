using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;

namespace Helper.Data.Core.Interfaces
{
    public interface IDbFactory
    {
        DbConnection CreateConnection(string connectionString);
        DbCommand CreateCommand(DbConnection conn);
        DbTransaction CreateTraction(DbConnection conn);
    }
}
