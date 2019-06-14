using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using System;

using Debug = UnityEngine.Debug;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Build.Reporting;

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

        private const string WindowsExtension = "exe";
        private const string MacExtension = "app";
        private const string LinuxExtension = "x86";
        private const string AndroidExtension = "apk";
        private const string WebGLIndexFile = "index.html";
        private const string ArchiveExtension = "zip";

        private static List<Service> services = null;

        public static string CurrentPlatform
        {
            get
            {
                string platform = EditorPrefs.GetString(PlayerSettings.productGUID + "_buildPlatform", "win");
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

        private static string GetParent(string path)
        {
            path = path.Replace("\\", "/");
            int last = path.LastIndexOf('/');
            return path.Substring(0, last);
        }

        [PostProcessBuild(1)]
        public static void OnPostprocessBuild(BuildTarget target, string path)
        {
            string platform = TargetToPlatform(target);
            string version = GetBuiltVersion(platform);

            string root = Path.GetFullPath(Settings.File.BuildsDirectory);

            //only trim on windows
            int os = (int)Environment.OSVersion.Platform;
            if (os != 4 && os != 6 && os != 128)
            {
                path = path.TrimStart('/');
            }

            path = GetParent(path);

            DateTime buildTime = DateTime.Now;
            string date = buildTime.ToString();
            date = date.Replace("/", "-");
            date = date.Replace(":", "-");
            date = date.Replace(" ", "_");

            //webgl builds to a folder instead of a file
            //so directly reference the folder 
            if (target == BuildTarget.WebGL)
            {
                path = path.TrimEnd('/');
                path += "/webgl";
            }

            //ensure the folder builds exists
            if (!Directory.Exists(root + "/" + platform))
            {
                Directory.CreateDirectory(root + "/" + platform);
            }

            string exportZip = Path.ChangeExtension(path, ArchiveExtension);
            string achiveFileName = Path.ChangeExtension(version + " (" + date + ")", ArchiveExtension);
            string archivedZipPath = Path.Combine(root, platform, achiveFileName);
            if (target == BuildTarget.Android)
            {
                exportZip = Path.Combine(path, Path.ChangeExtension(Settings.File.ExecutableName, AndroidExtension));
                archivedZipPath = Path.Combine(root, platform, achiveFileName);
            }
            else
            {
                //delete the old zip
                if (File.Exists(exportZip))
                {
                    File.Delete(exportZip);
                }
            }

            //call on post build after finishing building, but before archiving
            OnPostBuild(platform, path);

            //dont put android builds into an archive
            //theyre already a single file
            if (target != BuildTarget.Android)
            {
                Print("Compressing " + path + " to " + exportZip, MessageType.Info);
                Archiver.Zip(path, exportZip, platform);
                File.Copy(exportZip, archivedZipPath, true);
                EditorPrefs.SetString(Settings.File.GameName + "_builtArchive_" + platform, exportZip);
                Print("Exported archive to : " + exportZip, MessageType.Info);
            }
            else
            {
                EditorPrefs.SetString(Settings.File.GameName + "_builtArchive_" + platform, exportZip);
                //Debug.Log(exportZip + " > " + archivedZip);
                File.Copy(exportZip, archivedZipPath);
            }

            Building = false;
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
            string executableName = Settings.File.ExecutableName;
            string root = Path.GetFullPath(Settings.File.CurrentBuildDirectory);
            if (platform == "win") path = Path.Combine(root, platform, Path.ChangeExtension(executableName, WindowsExtension));
            if (platform == "mac") path = Path.Combine(root, platform, Path.ChangeExtension(executableName, MacExtension));
            if (platform == "linux") path = Path.Combine(root, platform, Path.ChangeExtension(executableName, LinuxExtension));
            if (platform == "webgl") path = Path.Combine(root, platform);
            if (platform == "android") path = Path.Combine(root, platform, Path.ChangeExtension(executableName, AndroidExtension));

            return path.Replace("\\", "/");
        }

        public static string GetBuildFolder(string platform)
        {
            string root = Path.GetFullPath(Settings.File.CurrentBuildDirectory);
            return Path.Combine(root, platform).Replace("\\", "/");
        }

        public static string GetBuiltPath(string platform)
        {
            return EditorPrefs.GetString(Settings.File.GameName + "_builtArchive_" + platform);
        }

        public static string GetPlayPath(string platform)
        {
            string path = "";
            string executableName = Settings.File.ExecutableName;
            string root = Path.GetFullPath(Settings.File.CurrentBuildDirectory);
            if (platform == "win") path = Path.Combine(root, platform, Path.ChangeExtension(executableName, WindowsExtension));
            if (platform == "mac") path = Path.Combine(root, platform, Path.ChangeExtension(executableName, MacExtension));
            if (platform == "linux") path = Path.Combine(root, platform, Path.ChangeExtension(executableName, LinuxExtension));
            if (platform == "webgl") path = Path.Combine(root, platform, WebGLIndexFile);
            if (platform == "android") path = Path.Combine(root, platform, Path.ChangeExtension(executableName, AndroidExtension));

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

            //ensure that the directory exists
            string folder = GetBuildFolder(platform);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            EditorPrefs.SetString(Settings.File.GameName + "_builtVersion_" + platform, Settings.CurrentVersion);

            //create the scenes array using the build settings
            string[] scenes = new string[EditorBuildSettings.scenes.Length];
            for (int i = 0; i < EditorBuildSettings.scenes.Length; i++)
            {
                EditorBuildSettingsScene scene = EditorBuildSettings.scenes[i];
                scenes[i] = scene.path;
            }

            //rebuild folder by deleting
            //and then by creating a new one

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
                report = BuildPipeline.BuildPlayer(scenes, path, target, options);
                if (report.summary.result == BuildResult.Failed)
                {
                    //il2cpp failed, so try mono
                    PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);
                    report = BuildPipeline.BuildPlayer(scenes, path, target, options);
                }
            }
            else
            {
                PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);
                report = BuildPipeline.BuildPlayer(scenes, path, target, options);
            }

            //success
            if (report.summary.result != BuildResult.Succeeded)
            {
                Building = false;
            }
        }

        private static void CallAll(string methodName, string namepace = null, params object[] arguments)
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
                        if (namepace == null || type.Namespace.StartsWith(namepace))
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

            //call the OnPreBuild method from any namespace
            CallAll(methodName, null, null);

            //call the addressable systems method
            CallAll("AddressableAssetSettings.BuildPlayerContent", null, null);
        }

        private static void OnPostBuild(string platform, string path)
        {
            const string methodName = "OnPostBuild";
            CallAll(methodName, null, platform, path);

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
            string outputPath = Path.Combine(GetParent(path), "Output.txt");

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
