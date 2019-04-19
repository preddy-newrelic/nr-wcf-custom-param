using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Custom.Providers.Wrapper.CustomWCF
{
    public static class Inspect
    {
        public static void GetAllProperties(object o)
        {
            BindingFlags flags = BindingFlags.FlattenHierarchy | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public | BindingFlags.Instance;

            PropertyInfo[] props = o.GetType().GetProperties(flags);
            foreach (PropertyInfo p in props)
            {
                Console.WriteLine(p);
            }
        }

        public static void GetAllFields (object o)
        {
            BindingFlags flags = BindingFlags.FlattenHierarchy | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public | BindingFlags.Instance;

            FieldInfo[] props = o.GetType().GetFields(flags);
            foreach (FieldInfo p in props)
            {
                Console.WriteLine(p);
            }
        }

        public static void GetAllMethods(object o)
        {
            BindingFlags flags = BindingFlags.FlattenHierarchy | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public | BindingFlags.Instance;

            MethodInfo[] methods = o.GetType().GetMethods(flags);
            foreach (MethodInfo p in methods)
            {
                Console.WriteLine(p);
            }
        }

        public static void GetAllMembers(object o)
        {
            BindingFlags flags = BindingFlags.FlattenHierarchy | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public | BindingFlags.Instance;

            MemberInfo[] members = o.GetType().GetMembers(flags);
            Dictionary<String, MemberTypes> list = new Dictionary<string, MemberTypes>();
            foreach (MemberInfo member in members)
            {
                string name = member.Name;
                MemberTypes tp = member.MemberType;
                list.Add(name, tp);
            }
        }
    }
}
