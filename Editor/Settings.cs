using System;
using System.Collections.Generic;
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
        private const string GameNameKey = "Popcron.Builder.Settings.GameName";
        private const string ExecutableNameKey = "Popcron.Builder.Settings.ExecutableName";
        private const string BlacklistedDirectoriesKey = "Popcron.Builder.Settings.BlacklistedDirectories";
        private const string ShowBlacklistedDirectoriesKey = "Popcron.Builder.Settings.ShowBlacklistedDirectories";
        private const string BlacklistedFilesKey = "Popcron.Builder.Settings.BlacklistedFiles";
        private const string ShowBlacklistedFilesKey = "Popcron.Builder.Settings.ShowBlacklistedFiles";
        
        private static List<string> blacklistedDirectories = null;
        private static List<string> blacklistedFiles = null;

        /// <summary>
        /// Name of the game
        /// </summary>
        public static string GameName
        {
            get
            {
                return EditorPrefs.GetString(PlayerSettings.productGUID + GameNameKey, PlayerSettings.productName);
            }
            set
            {
                EditorPrefs.SetString(PlayerSettings.productGUID + GameNameKey, value);
            }
        }

        /// <summary>
        /// Name of the executable
        /// </summary>
        public static string ExecutableName
        {
            get
            {
                return EditorPrefs.GetString(PlayerSettings.productGUID + ExecutableNameKey, PlayerSettings.productName);
            }
            set
            {
                EditorPrefs.SetString(PlayerSettings.productGUID + ExecutableNameKey, value);
            }
        }

        /// <summary>
        /// List of directories to exclude when archiving the game
        /// </summary>
        public static List<string> BlacklistedDirectories
        {
            get
            {
                if (blacklistedDirectories == null)
                {
                    string value = EditorPrefs.GetString(PlayerSettings.productGUID + BlacklistedDirectoriesKey);
                    if (string.IsNullOrEmpty(value))
                    {
                        blacklistedDirectories = new List<string>();
                    }
                    else if (value.Contains('\n'))
                    {
                        blacklistedDirectories = value.Split('\n').ToList();
                    }
                    else
                    {
                        blacklistedDirectories = new List<string>() { value };
                    }
                }

                return blacklistedDirectories;
            }
            set
            {
                if (value == null || value.Count == 0)
                {
                    blacklistedDirectories = new List<string>();
                    EditorPrefs.DeleteKey(PlayerSettings.productGUID + BlacklistedDirectoriesKey);
                }
                else if (blacklistedDirectories == null || !Enumerable.SequenceEqual(blacklistedDirectories, value))
                {
                    blacklistedDirectories = new List<string>();
                    blacklistedDirectories.AddRange(value);

                    EditorPrefs.SetString(PlayerSettings.productGUID + BlacklistedDirectoriesKey, string.Join("\n", value));
                }
            }
        }

        internal static bool ShowBlacklistedDirectories
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

        public static List<string> BlacklistedFiles
        {
            get
            {
                if (blacklistedFiles == null)
                {
                    string value = EditorPrefs.GetString(PlayerSettings.productGUID + BlacklistedFilesKey);
                    if (string.IsNullOrEmpty(value))
                    {
                        blacklistedFiles = new List<string>();
                    }
                    else if (value.Contains('\n'))
                    {
                        blacklistedFiles = value.Split('\n').ToList();
                    }
                    else
                    {
                        blacklistedFiles = new List<string>() { value };
                    }
                }

                return blacklistedFiles;
            }
            set
            {
                if (value == null || value.Count == 0)
                {
                    blacklistedFiles = new List<string>();
                    EditorPrefs.DeleteKey(PlayerSettings.productGUID + BlacklistedFilesKey);
                }
                else if (blacklistedFiles == null || !Enumerable.SequenceEqual(blacklistedFiles, value))
                {
                    blacklistedFiles = new List<string>();
                    blacklistedFiles.AddRange(value);

                    EditorPrefs.SetString(PlayerSettings.productGUID + BlacklistedFilesKey, string.Join("\n", value));
                }
            }
        }

        internal static bool ShowBlacklistedFiles
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

                if (now.Month == 1) month = "fb";
                else if (now.Month == 2) month = "jy";
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