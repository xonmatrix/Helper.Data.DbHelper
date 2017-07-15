using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Helper.Data
{
    public interface IDbQuery : IDisposable
    {
        IDbQuery Append(string text, params object[] parameter);
        IDbQuery Append(string text);
        IDbQuery And(string text, params object[] parameter);
        IDbQuery Where(string query, params object[] parameters);

        string AppendValue(object value);
        string AppendCondition(object value);

        Task<T> SingleOrDefault<T>(Func<IDataReader, T> map);
        Task<T> SingleOrDefault<T>();
        Task<DbModel> SingleOrDefault();
        Task<T> Value<T>();
        Task<bool> Any();

        Task<List<T>> Select<T>(Func<IDataReader, T> rowReader);
        Task<List<DbModel>> ToList();
        Task<List<T>> ToList<T>();
        Task<JObject> ToJObject();
        Task<JArray> ToJArray();

        Task ExecuteReader(Func<IDataReader, bool> rowExecution);
        Task ExecuteNonQuery();
    }
}
