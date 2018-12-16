using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Popcron.Builder
{
    public abstract class Service
    {
        private const string ShowKey = "Popcron.Builder.Service";

        private static Dictionary<string, Type> typeToType = null;

        public string Name { get; set; } = "";

        public int Index { get; set; } = 0;

        public abstract string Type { get; }
        public abstract bool CanUploadTo { get; set; }
        public abstract string URL { get; }

        public abstract Task Upload(string path, string version, string platform);
        public virtual void OnGUI() { }

        public static List<(string typeName, Type systemType)> Services
        {
            get
            {
                List<(string typeName, Type systemType)> list = new List<(string typeName, Type systemType)>();

                if (typeToType == null)
                {
                    Get("", "");
                }

                for (int i = 0; i < typeToType.Count; i++)
                {
                    string typeName = typeToType.Keys.ElementAt(i);
                    Type systemType = typeToType.Values.ElementAt(i);
                    list.Add((typeName, systemType));
                }

                return list;
            }
        }

        public static Service Get(string type, string name)
        {
            if (typeToType == null)
            {
                //load all services
                typeToType = new Dictionary<string, Type>();
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assembly in assemblies)
                {
                    Type[] types = assembly.GetTypes();
                    foreach (var t in types)
                    {
                        if (t.IsAbstract) continue;
                        if (t.IsSubclassOf(typeof(Service)))
                        {
                            Service service = Activator.CreateInstance(t) as Service;
                            typeToType.Add(service.Type, t);
                        }
                    }
                }
            }

            if (typeToType.TryGetValue(type, out Type serviceType))
            {
                Service service = Activator.CreateInstance(serviceType) as Service;
                service.Name = name;

                return service;
            }
            else
            {
                EmptyService empty = new EmptyService
                {
                    Name = name
                };

                return empty;
            }
        }
    }
}
