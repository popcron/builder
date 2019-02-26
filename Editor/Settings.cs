using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Popcron.Builder
{

    public class Settings
    {
        private const string ShowBlacklistedDirectoriesKey = "Popcron.Builder.Settings.ShowBlacklistedDirectories";
        private const string ShowBlacklistedFilesKey = "Popcron.Builder.Settings.ShowBlacklistedFiles";

        private static List<string> blacklistedDirectories = null;
        private static List<string> blacklistedFiles = null;
        private static SettingsFile file = null;

        public static SettingsFile File
        {
            get
            {
                string path = Directory.GetParent(Application.dataPath).FullName;
                path = Path.Combine(path, "ProjectSettings", "BuilderSettings.asset");
                bool exists = System.IO.File.Exists(path);
                if (file == null || !exists)
                {
                    file = SettingsFile.Create(path);
                }

                return file;
            }
        }

        public static bool ShowBlacklistedDirectories
        {
            get
            {
                return EditorPrefs.GetBool(PlayerSettings.productGUID + ShowBlacklistedDirectoriesKey, false);
            }
            set
            {
                EditorPrefs.SetBool(PlayerSettings.productGUID + ShowBlacklistedDirectoriesKey, value);
            }
        }

        public static bool ShowBlacklistedFiles
        {
            get
            {
                return EditorPrefs.GetBool(PlayerSettings.productGUID + ShowBlacklistedFilesKey, false);
            }
            set
            {
                EditorPrefs.SetBool(PlayerSettings.productGUID + ShowBlacklistedFilesKey, value);
            }
        }

        public static string CurrentVersion
        {
            get
            {
                string month = "-";
                DateTime now = DateTime.Now;

                if (now.Month == 1) month = "jy";
                else if (now.Month == 2) month = "fb";
                else if (now.Month == 3) month = "mr";
                else if (now.Month == 4) month = "ap";
                else if (now.Month == 5) month = "my";
                else if (now.Month == 6) month = "jn";
                else if (now.Month == 7) month = "jl";
                else if (now.Month == 8) month = "ag";
                else if (now.Month == 9) month = "sp";
                else if (now.Month == 10) month = "oc";
                else if (now.Month == 11) month = "nv";
                else if (now.Month == 12) month = "dc";

                string day = now.Day.ToString("00");
                int hour = now.Hour;

                return now.Year + month + day + "-" + hour + "." + now.Minute.ToString("00");
            }
        }

        public static string LastVersion
        {
            get
            {
                string typeName = "Popcron.Builder.GameInfo, Assembly-CSharp";
                Type type = Type.GetType(typeName);
                if (type != null)
                {
                    FieldInfo property = type.GetField("Version", BindingFlags.Public | BindingFlags.Static);
                    return (string)property.GetRawConstantValue();
                }

                return "null";
            }
        }
    }
}
