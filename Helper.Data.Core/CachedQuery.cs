using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;
using System.Threading.Tasks;
using System.Data;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Helper.Data
{
    public class CachedQuery
    {
        private Dictionary<int, DbModel> cache = new Dictionary<int, DbModel>();
        private DbCommand cmd;
        private DbParameter param;
        private List<string> jsonFields;

        internal CachedQuery(DbCommand cmd)
        {
            this.cmd = cmd;
        }

        public void Prepare(string cmdString)
        {
            this.param = this.cmd.CreateParameter();
            cmd.CommandText = cmdString.Replace("{0}", "@id");
            param.ParameterName = "@id";
            param.Direction = System.Data.ParameterDirection.Input;
            param.DbType = System.Data.DbType.Int32;
            this.cmd.Parameters.Add(param);
            if (cmd.Connection.State != ConnectionState.Open)
                cmd.Connection.Open();

            this.cmd.Prepare();
        }

        public async Task<DbModel> Get(int id)
        {
            DbModel result = null;
            if (cache.TryGetValue(id, out result))
            {
                Console.WriteLine("CACHE!");
                return result;
            }

            param.Value = id;
            using (var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false))
            {
                if (reader.Read())
                {
                    Console.WriteLine("Real!");
                    result = mapToDbModel(reader);
                    cache.Add(id, result);
                    return result;
                }
                return null;
            }
        }

        public CachedQuery WithJsonField(params string[] fieldName)
        {
            if (jsonFields == null)
                jsonFields = new List<string>();

            jsonFields.AddRange(fieldName.Select(a => a.ToLower()));
            return this;
        }

        private DbModel mapToDbModel(DbDataReader reader)
        {
            DbModel result = new DbModel();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (reader.IsDBNull(i))
                    result[reader.GetName(i)] = null;
                else if (isJsonField(reader.GetName(i)))
                    result[reader.GetName(i)] = JValue.Parse(reader.GetString(i));
                else
                {
                    var value = reader[i];
                    if (value is DateTime)
                        result[reader.GetName(i)] = DateTime.SpecifyKind(reader.GetDateTime(i), DateTimeKind.Local);
                    else
                        result[reader.GetName(i)] = reader[i];
                }
            }
            return result;
        }

        private bool isJsonField(string fieldName)
        {
            if (this.jsonFields == null)
                return false;
            else return this.jsonFields.Contains(fieldName.ToLower());
        }

        public void Dispose()
        {
            Console.WriteLine("Disposed!");
            cache.Clear();
            cache = null;
            this.cmd.Dispose();
            this.cmd = null;
        }
    }
}
