using System;
using System.Collections.Generic;
using System.Dynamic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Helper.Data
{
    [JsonConverter(typeof(DbModelConverter))]
    public class DbModel : DynamicObject
    {
        public Dictionary<string, object> Data { get; private set; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        public object this[string name]
        {
            set
            {
                Data[name] = value;
            }
            get
            {
                if (Data.TryGetValue(name, out object storedValue))
                {
                    return storedValue;
                }
                else
                    return null;
            }
        }

        public List<DbModel> List(string name)
        {
            return this.List<DbModel>(name);
        }

        public List<T> List<T>(string name)
        {
            object i = this[name];
            if (i == null)
                return new List<T>();
            if (i.GetType() == typeof(JArray))
            {
                List<T> t = new List<T>();
                foreach (var j in (JArray)i)
                {
                    t.Add(j.ToObject<T>());
                }
                return t;
            }
            else
                return (List<T>)i;
        }

        public int Int(string name)
        {
            object i = this[name];
            if (i == null)
                return default(int);
            else
                return Convert.ToInt32(i);
        }

        public string String(string name)
        {
            object i = this[name];
            if (i == null)
                return "";
            else
                return i.ToString();
        }

        public bool Bool(string name)
        {
            object i = this[name];
            if (i == null)
                return false;
            else if (i.GetType() == typeof(sbyte))
                return ((sbyte)i) > 0;
            else if (i.GetType() == typeof(byte))
                return ((byte)i) > 0;
            else
                return (bool)i;
        }

        public decimal Decimal(string name)
        {
            object i = this[name];
            if (i == null)
                return 0;
            else if (i.GetType() == typeof(decimal))
                return (decimal)i;
            else
                return Convert.ToDecimal(i); //try to convert
        }

        public DateTime? DateTime(string name)
        {
            object i = this[name];
            if (i == null)
                return null;
            else if (i.GetType() == typeof(DateTime))
            {
                var dt = (DateTime)i;
                if (dt.Kind == DateTimeKind.Utc)
                    return dt.ToLocalTime();
                else
                    return dt;
            }
            else if (i.GetType() == typeof(int))
            {
                return DateTimeOffset.FromUnixTimeSeconds((long)i).LocalDateTime;
            }
            else if(i.GetType() == typeof(string))
            {
                if (System.DateTime.TryParse(i.ToString(), out var dt))
                {
                    return dt;
                }
                else
                    return null;
            }
            else
                return null;
        }

        public DbModel Merge(DbModel source)
        {
            foreach(var key in source.Data)
            {
                this[key.Key] = key.Value;
            }
            return this;
        }

        public void RemoveProperties(params string[] props)
        {
            foreach (var p in props)
            {
                if (Data.ContainsKey(p))
                    Data.Remove(p);
            }
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            return Data.TryGetValue(binder.Name, out result);
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            Data[binder.Name] = value;
            return true;
        }
    }

    public class DbModelConverter : JsonConverter
    {
        public override bool CanRead
        {
            get
            {
                return true;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return true;
            }
        }

        public override bool CanConvert(Type objectType)
        {
            return true;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;
            else if (reader.TokenType == JsonToken.StartObject)
            {
                DbModel result = new DbModel();
                while (reader.Read())
                {
                    if (reader.TokenType == JsonToken.PropertyName)
                    {
                        string name = reader.Value.ToString();
                        reader.Read();
                        if (reader.TokenType == JsonToken.Date)
                        {
                            DateTime dt = (DateTime)reader.Value;
                            if (dt.Kind == DateTimeKind.Utc)
                                result[name] = dt.ToLocalTime();
                            else
                                result[name] = dt;
                        }
                        else if (reader.TokenType == JsonToken.Integer)
                        {
                            result[name] = serializer.Deserialize<int>(reader);
                        }
                        else
                            result[name] = serializer.Deserialize(reader);
                    }
                }
                return result;
            }
            else
                throw new FormatException("Invalid json format for dbModel");
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            DbModel model = (DbModel)value;
            foreach (var pair in model.Data)
            {
                writer.WritePropertyName(pair.Key);
                serializer.Serialize(writer, pair.Value, value.GetType());
            }

            writer.WriteEndObject();
        }

    }
}
