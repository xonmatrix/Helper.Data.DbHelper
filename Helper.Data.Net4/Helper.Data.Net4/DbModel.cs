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
                return (sbyte)i > 0;
            else if (i.GetType() == typeof(byte))
                return (byte)i > 0;
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
                return Convert.ToDecimal(name); //try to convert
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

}
