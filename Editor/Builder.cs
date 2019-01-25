using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using System;

using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Build.Reporting;
using UnityEngine.SceneManagement;

namespace Popcron.Builder
{
    public class Builder : EditorWindow
    {
        private const string PlayOnBuildKey = "Popcron.Builder.PlayOnBuild";
        private const string BuildingKey = "Popcron.Builder.Building";
        private const string ProfilerDebugKey = "Popcron.Builder.ProfilerDebug";
        private const string UploadingKey = "Popcron.Builder.Uploading";
        private const string BuildModeKey = "Popcron.Builder.BuildMode";
        private const string LogKey = "Popcron.Builder.Log";
        private const string ServicesKey = "Popcron.Builder.Services";

        private static List<Service> services = null;

        public static string CurrentPlatform
        {
            get
            {
                string platform = EditorPrefs.GetString(Settings.File.GameName + "_buildPlatform", "win");
                return platform;
            }
        }

        public static bool PlayOnBuild
        {
            get
            {
                return EditorPrefs.GetBool(PlayerSettings.productGUID + PlayOnBuildKey, false);
            }
            set
            {
                EditorPrefs.SetBool(PlayerSettings.productGUID + PlayOnBuildKey, value);
            }
        }

        public static bool Uploading
        {
            get
            {
                return EditorPrefs.GetBool(PlayerSettings.productGUID + UploadingKey, false);
            }
            set
            {
                EditorPrefs.SetBool(PlayerSettings.productGUID + UploadingKey, value);
            }
        }

        public static bool Building
        {
            get
            {
                return EditorPrefs.GetBool(PlayerSettings.productGUID + BuildingKey, false);
            }
            set
            {
                EditorPrefs.SetBool(PlayerSettings.productGUID + BuildingKey, value);
            }
        }

        public static ScriptingImplementation ScriptingImplementation
        {
            get
            {
                return (ScriptingImplementation)EditorPrefs.GetInt(PlayerSettings.productGUID + BuildModeKey, 0);
            }
            set
            {
                EditorPrefs.SetInt(PlayerSettings.productGUID + BuildModeKey, (int)value);
            }
        }

        public static bool ProfilerDebug
        {
            get
            {
                return EditorPrefs.GetBool(PlayerSettings.productGUID + ProfilerDebugKey, false);
            }
            set
            {
                EditorPrefs.SetBool(PlayerSettings.productGUID + ProfilerDebugKey, value);
            }
        }

        public static List<Service> Services
        {
            get
            {
                if (services == null)
                {
                    services = new List<Service>();
                    string value = EditorPrefs.GetString(PlayerSettings.productGUID + ServicesKey, "");
                    if (string.IsNullOrEmpty(value))
                    {
                        services = new List<Service>();
                    }
                    else if (!value.Contains(":"))
                    {
                        string type = value.Split('+')[0];
                        string name = value.Split('+')[1];

                        Service service = Service.Get(type, name);
                        services.Add(service);
                    }
                    else
                    {
                        string[] array = value.Split(':');
                        for (int i = 0; i < array.Length; i++)
                        {
                            string type = array[i].Split('+')[0];
                            string name = array[i].Split('+')[1];

                            Service service = Service.Get(type, name);
                            services.Add(service);
                        }
                    }
                }

                return services;
            }
            set
            {
                if (value == null || value.Count == 0)
                {
                    EditorPrefs.DeleteKey(PlayerSettings.productGUID + ServicesKey);
                }
                else
                {
                    string[] lines = new string[value.Count];
                    for (int i = 0; i < value.Count; i++)
                    {
                        string type = value[i].Type;
                        string name = value[i].Name;
                        lines[i] = type + "+" + name;
                    }

                    string data = string.Join(":", lines);
                    EditorPrefs.SetString(PlayerSettings.productGUID + ServicesKey, data);
                }
            }
        }

        public static List<Service> GetServices()
        {
            services = null;
            return Services;
        }

        internal static void ClearLog()
        {
            EditorPrefs.SetString(PlayerSettings.productGUID + LogKey, "");
        }

        internal static List<(string text, MessageType type)> Log
        {
            get
            {
                const char Delimeter = (char)1;
                string value = EditorPrefs.GetString(PlayerSettings.productGUID + LogKey, "");
                string[] array = value.Split('\n');
                List<(string text, MessageType type)> list = new List<(string text, MessageType type)>();
                for (int i = 0; i < array.Length; i++)
                {
                    if (array[i].Contains(Delimeter))
                    {
                        string elementLine = array[i].Split(Delimeter)[0];
                        MessageType elementType = (MessageType)int.Parse(array[i].Split(Delimeter)[1]);
                        list.Add((elementLine, elementType));
                    }
                }

                return list;
            }
        }

        internal static void Print(string text, MessageType type)
        {
            const char Delimeter = (char)1;
            var list = Log;
            list.Add((text, type));
            if (list.Count > 10)
            {
                int extra = list.Count - 10;
                for (int i = 0; i < extra; i++)
                {
                    list.RemoveAt(0);
                }
            }

            string[] data = new string[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                data[i] = list[i].text + Delimeter + ((int)list[i].type);
            }

            string pref = string.Join("\n", data);
            EditorPrefs.SetString(PlayerSettings.productGUID + LogKey, pref);
        }

        internal static void Reset()
        {
            PlayOnBuild = false;
            Uploading = false;
            Building = false;
        }

        [PostProcessBuild(1)]
        public static void OnPostprocessBuild(BuildTarget target, string path)
        {
            string platform = TargetToPlatform(target);
            string version = GetBuiltVersion(platform);

            string root = Settings.File.BuildsDirectory;
            if (path.StartsWith("/"))
            {
                path = path.Substring(1);
            }
            path = Directory.GetParent(path).FullName;

            DateTime buildTime = DateTime.Now;
            string date = buildTime.ToString();
            date = date.Replace("/", "-");
            date = date.Replace(":", "-");
            date = date.Replace(" ", "_");

            //webgl builds to a folder instead of a file
            //so directly reference the folder 
            if (target == BuildTarget.WebGL)
            {
                path += "/webgl";
            }

            //ensure the folder builds exists
            if (!Directory.Exists(root + "/" + platform))
            {
                Directory.CreateDirectory(root + "/" + platform);
            }

            string exportZip = path + ".zip";
            string archivedZip = root + "/" + platform + "/" + version + " (" + date + ").zip";
            if (target == BuildTarget.Android)
            {
                exportZip = path + "/" + Settings.File.ExecutableName + ".apk";
                archivedZip = root + "/" + platform + "/" + version + " (" + date + ").apk";

            }
            else
            {
                //delete the old zip
                if (File.Exists(exportZip))
                {
                    File.Delete(exportZip);
                }
            }

            //dont put android builds into an archive
            //theyre already a single file
            if (target != BuildTarget.Android)
            {
                Print("Compressing " + path + " to " + exportZip, MessageType.Info);
                if (target == BuildTarget.WebGL)
                {
                    Archiver.Zip(path + "/" + Settings.File.ExecutableName, exportZip, platform);
                }
                else
                {
                    Archiver.Zip(path, exportZip, platform);
                    File.Copy(exportZip, archivedZip);
                }

                EditorPrefs.SetString(Settings.File.GameName + "_builtArchive_" + platform, exportZip);
                Print("Exported archive to : " + exportZip, MessageType.Info);
            }
            else
            {
                EditorPrefs.SetString(Settings.File.GameName + "_builtArchive_" + platform, exportZip);
                //UnityEngine.Debug.Log(exportZip + " > " + archivedZip);
                File.Copy(exportZip, archivedZip);
            }

            Building = false;
            OnPostBuild(platform);
        }

        public static BuildTarget PlatformToTarget(string platform)
        {
            if (platform == "win") return BuildTarget.StandaloneWindows64;
            if (platform == "linux") return BuildTarget.StandaloneLinuxUniversal;
            if (platform == "mac") return BuildTarget.StandaloneOSX;
            if (platform == "webgl") return BuildTarget.WebGL;
            if (platform == "android") return BuildTarget.Android;

            throw new Exception(platform + " is not supported.");
        }

        public static string TargetToPlatform(BuildTarget target)
        {
            if (target == BuildTarget.StandaloneWindows64) return "win";
            if (target == BuildTarget.StandaloneOSX) return "mac";
            if (target == BuildTarget.StandaloneLinuxUniversal) return "linux";
            if (target == BuildTarget.WebGL) return "webgl";
            if (target == BuildTarget.Android) return "android";

            throw new Exception(target + " is not supported.");
        }

        public static string GetBuildPath(string platform)
        {
            string path = "";
            string exec = Settings.File.ExecutableName;
            string root = Settings.File.CurrentBuildDirectory;
            if (platform == "win") path = root + "/" + platform + "/" + exec + ".exe";
            if (platform == "mac") path = root + "/" + platform + "/" + exec + ".app";
            if (platform == "linux") path = root + "/" + platform + "/" + exec + ".x86";
            if (platform == "webgl") path = root + "/" + platform;
            if (platform == "android") path = root + "/" + platform + "/" + exec + ".apk";

            return path.Replace("\\", "/");
        }

        public static string GetBuiltPath(string platform)
        {
            return EditorPrefs.GetString(Settings.File.GameName + "_builtArchive_" + platform);
        }

        public static string GetPlayPath(string platform)
        {
            string path = "";
            string exec = Settings.File.ExecutableName;
            string root = Settings.File.CurrentBuildDirectory + "/" + platform + "/";
            if (platform == "win") path = root + exec + ".exe";
            if (platform == "mac") path = root + exec + ".app";
            if (platform == "linux") path = root + exec + ".x86";
            if (platform == "webgl") path = root + "/index.html";
            if (platform == "android") path = root + exec + ".apk";

            return path.Replace("\\", "/");
        }

        public static void BuildAndPlay(string platform)
        {
            PlayOnBuild = true;
            Build(platform);
        }

        public static void Build(string platform)
        {
            Building = true;
            BuildTarget target = PlatformToTarget(platform);
            string path = GetBuildPath(platform);

            EditorPrefs.SetString(Settings.File.GameName + "_builtVersion_" + platform, Settings.CurrentVersion);

            Scene activeScene = SceneManager.GetActiveScene();
            string[] levels = new string[] { activeScene.path };

            //ensure that the directory exists
            if (!Directory.Exists(Settings.File.CurrentBuildDirectory))
            {
                Directory.CreateDirectory(Settings.File.CurrentBuildDirectory);
            }

            //rebuild folder by deleting
            //and then by creating a new one
            string folder = Settings.File.CurrentBuildDirectory + "/" + platform;
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, true);
            }
            Directory.CreateDirectory(folder);

            //compile gameinfo file
            GameInfoGenerator.CompileGameInfo(platform, Settings.CurrentVersion);

            //find all scripts that have an OnPreBuild method, and call it
            OnPreBuild();

            BuildOptions options = BuildOptions.CompressWithLz4HC | BuildOptions.AcceptExternalModificationsToPlayer;
            if (ProfilerDebug)
            {
                options |= BuildOptions.ConnectWithProfiler;
            }

            //if on android, set build mode to mono and use custom options
            if (platform == "android")
            {
                PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);
                options = BuildOptions.None;
            }

            //UnityEngine.Debug.Log("path: " + path + ", target: " + target + ", options: " + options);
            BuildReport report = null;
            if (ScriptingImplementation == ScriptingImplementation.IL2CPP)
            {
                //try to use il2cpp
                PlayerSettings.SetIncrementalIl2CppBuild(BuildTargetGroup.Standalone, true);
                PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.IL2CPP);
                report = BuildPipeline.BuildPlayer(levels, path, target, options);
                if (report.summary.result == BuildResult.Failed)
                {
                    //il2cpp failed, so try mono
                    PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);
                    report = BuildPipeline.BuildPlayer(levels, path, target, options);
                }
            }
            else
            {
                PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);
                report = BuildPipeline.BuildPlayer(levels, path, target, options);
            }

            //success
            if (report.summary.result != BuildResult.Succeeded)
            {
                Building = false;
            }
        }

        private static void CallAll(string methodName, params object[] arguments)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                var types = assembly.GetTypes();
                foreach (var type in types)
                {
                    if (type == typeof(Builder)) continue;
                    if (type.Namespace != null)
                    {
                        if (type.Namespace.StartsWith("Popcron"))
                        {
                            var methods = type.GetMethods();
                            foreach (var method in methods)
                            {
                                if (method.Name == methodName)
                                {
                                    method.Invoke(null, arguments);
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void OnPreBuild()
        {
            const string methodName = "OnPreBuild";
            CallAll(methodName, null);
        }

        private static void OnPostBuild(string platform)
        {
            const string methodName = "OnPostBuild";
            CallAll(methodName, platform);

            if (PlayOnBuild)
            {
                Play(platform);
                PlayOnBuild = false;
            }
        }

        public static string GetBuiltVersion(string platform)
        {
            return EditorPrefs.GetString(Settings.File.GameName + "_builtVersion_" + platform);
        }

        public static string GetUploadVersion(string platform)
        {
            return EditorPrefs.GetString(Settings.File.GameName + "_uploadVersion_" + platform);
        }

        public static async void Upload(string platform)
        {
            Uploading = true;
            string path = GetBuiltPath(platform);
            string version = GetBuiltVersion(platform);

            EditorPrefs.SetString(Settings.File.GameName + "_uploadedPlatform_", platform);
            EditorPrefs.SetString(Settings.File.GameName + "_uploadVersion_" + platform, version);

            //run through list of services
            //and upload to the ones that are allowed
            List<Task> tasks = new List<Task>();
            List<Service> services = Services;
            for (int i = 0; i < services.Count; i++)
            {
                if (services[i].CanUploadTo)
                {
                    Print("Started uploading to " + services[i].Name, MessageType.Info);
                    var task = services[i].Upload(path, version, platform);
                    tasks.Add(task);
                }
            }

            await Task.WhenAll(tasks);
            Uploading = false;
        }

        public static void Play(string platform)
        {
            string path = GetPlayPath(platform);
            string outputPath = Directory.GetParent(path).FullName + "/Output.txt";

            Process gameProcess = new Process();

            gameProcess.StartInfo.CreateNoWindow = true;
            gameProcess.StartInfo.FileName = path;
            gameProcess.StartInfo.Arguments = "-logfile \"" + outputPath + "\"";
            gameProcess.Start();
            Print("Launched game from " + path, MessageType.Info);
        }

        public static bool PlayExists(string platform)
        {
            if (platform == "mac") return Directory.Exists(GetPlayPath(platform));

            return File.Exists(GetPlayPath(platform));
        }

        public static bool UploadExists(string platform)
        {
            return File.Exists(GetBuiltPath(platform));
        }
    }
}