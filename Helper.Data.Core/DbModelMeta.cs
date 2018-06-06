using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace Helper.Data
{
    public class DbModelMeta
    {
        private List<string> ignoredColumns = new List<string>();
        private Dictionary<string, string> fieldNameMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, Func<object, Task<object>>> valueMapperTask = new Dictionary<string, Func<object, Task<object>>>(StringComparer.OrdinalIgnoreCase);

        public DbModelMeta IgnoreProperty(params string[] propertyName)
        {
            this.ignoredColumns.AddRange(propertyName);
            return this;
        }

        public DbModelMeta MapFieldName(string from, string to)
        {
            fieldNameMap.Add(from, to);
            return this;
        }

        public DbModelMeta MapValueAsync(string key, Func<object, Task<object>> mapper)
        {
            this.valueMapperTask.Add(key, mapper);
            return this;
        }

        public async Task<JObject> Print(DbModel model)
        {
            JObject res = new JObject();
            foreach(var key in model.Data.Keys)
            {
                if (this.ignoredColumns.Contains(key))
                    continue;

                var shouldMapVal = valueMapperTask.TryGetValue(key, out Func<object, Task<object>> mapValueTask);
                var shouldMapName = fieldNameMap.TryGetValue(key, out string keyName);

                res[shouldMapName ? keyName : key] = JToken.FromObject(shouldMapVal ? await mapValueTask(model[key]) : model[key]);
            }
            return res;
        }

        public async Task<Tuple<JObject,JObject>> CompareModel(DbModel source, DbModel destination)
        {
            foreach (var key in source.Data.Keys.ToArray().Intersect(this.ignoredColumns))
                source.Data.Remove(key);

            foreach (var key in destination.Data.Keys.ToArray().Intersect(this.ignoredColumns))
                destination.Data.Remove(key);


            Tuple<DbModel,DbModel> res = new Tuple<DbModel, DbModel>(new DbModel(), new DbModel());

            //Property exists in source, but not dest.
            foreach (var key in source.Data.Keys.Except(destination.Data.Keys))
            {
                res.Item1[key] = source[key];
                res.Item2[key] = null;
            }

            //Property exists in dest but not in source.
            foreach (var key in destination.Data.Keys.Except(source.Data.Keys))
            {
                res.Item2[key] = destination[key];
                res.Item1[key] = null;
            }

            //Cross compare for same fields.
            foreach (var key in source.Data.Keys.Intersect(destination.Data.Keys))
            {
                if (isValueDiff(source[key], destination[key]))
                {
                    res.Item1[key] = source[key];
                    res.Item2[key] = destination[key];
                }
            }

            return new Tuple<JObject, JObject>(await Print(res.Item1), await Print(res.Item2));

            bool isValueDiff(object sVal,object dVal)
            {
                if (sVal == null && dVal == null)
                    return false ;

                if (sVal != null && dVal == null)
                    return true;

                if(sVal == null && dVal != null)
                    return true;

                if(sVal is IComparable)
                    return (((IComparable)sVal).CompareTo(dVal) != 0);
                
                if(sVal is JToken && dVal is JToken)
                    return !JToken.DeepEquals(sVal as JToken, dVal as JToken);
                

                return !sVal.Equals(dVal);
                        
            }
        }






    }
}
