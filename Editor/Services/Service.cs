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

        public bool Show
        {
            get
            {
                return EditorPrefs.GetBool(ShowKey + "." + Name, false);
            }
            set
            {
                EditorPrefs.SetBool(ShowKey + "." + Name, value);
            }
        }

        public abstract string Name { get; }
        public abstract bool CanUploadTo { get; set; }
        public abstract string URL { get; }

        public abstract Task Upload(string path, string version, string platform);
        public virtual void OnGUI() { }
    }
}
