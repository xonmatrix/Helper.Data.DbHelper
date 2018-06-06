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

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            return Data.TryGetValue(binder.Name, out result);
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            Data[binder.Name] = value;
            return true;
        }

        public List<string> CompareTo(DbModel b)
        {
            List<string> diff = new List<string>();
            foreach (var key in Data.Keys)
            {
                var val2 = b[key];
                var val1 = Data[key];
                if (val1 == null)
                {
                    if (val2 != null)
                        diff.Add(key);
                }
                else if (val2 == null)
                {
                    diff.Add(key);
                }
                else
                {
                    if (val1 is IComparable)
                    {
                        if (((IComparable)val1).CompareTo(val2) != 0)
                            diff.Add(key);
                    }
                    else if (!val1.Equals(val2))
                        diff.Add(key);

                }
            }

            return diff;
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
                            result[name] = (DateTime)reader.Value;
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
