using System;
using System.Collections.Generic;

namespace Tests.Contexts
{
    public class Super
    {
        public int x { get; set; }
        public bool? y { get; set; }
        public bool z { get; set; }
        public object DataContext { get; set; }

        public Dictionary<string, object> vars = new Dictionary<string, object>();
        public Dictionary<string, Type> types = new Dictionary<string, Type>();

        public int nullable(int? value)
        {
            return value.Value;
        }

        public Type getType(string name)
        {
            return types[name];
        }

        public object getVar(string name)
        {
            if (vars.ContainsKey(name))
            {
                return vars[name];
            }
            else
            {
                vars.Add(name, null);
                return null;
            }
        }

        public void setVar(string name, object value)
        {
            if (vars.ContainsKey(name))
            {
                vars[name] = value;
            }
            else
            {
                vars.Add(name, value);
            }

            setType(name, value.GetType());
        }

        private void setType(string name, Type type)
        {
            if (types.ContainsKey(name))
            {
                types[name] = type;
            }
            else
            {
                types.Add(name, type);
            }
        }
    }
}