using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Helper.Data
{
    public interface IDbHelper : IDisposable
    {
        Task Insert(string tableName, IEnumerable<DbModel> data);
        Task Insert(string tableName, DbModel data);
        Task<int> InsertWithIdentity(string tableName, DbModel data);
        Task Update(string tableName, DbModel data, string condition, params object[] parameters);
        Task Delete(string tableName, string condition, params object[] parameters);
        IDbQuery Query(string query, params object[] parameters);
        Task Execute(string query, params object[] parameters);

        void BeginTransaction();
        void Commit();
        void Rollback();
    }
}
