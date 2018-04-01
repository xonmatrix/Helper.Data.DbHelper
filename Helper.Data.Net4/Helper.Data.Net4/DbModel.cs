using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;
namespace Helper.Data.Net4
{


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
            else if (i.GetType() == typeof(SByte))
                return ((sbyte)i) > 0;
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

}
