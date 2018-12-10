using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;
using System.Diagnostics;
using UnityEditor.Callbacks;
using System;

//github stuff
using Application = UnityEngine.Application;
using Debug = UnityEngine.Debug;
using UnityEngine.SceneManagement;
using UnityEditor.Build.Reporting;
using System.Threading.Tasks;

namespace Popcron.Builder
{
    public class Builder : EditorWindow
    {
        private const string PlayOnBuildKey = "Popcron.Builder.PlayOnBuild";
        private const string BuildingKey = "Popcron.Builder.Building";
        private const string UploadingKey = "Popcron.Builder.Uploading";
        private const string BuildModeKey = "Popcron.Builder.BuildMode";
        private const string LogKey = "Popcron.Builder.Log";

        private static bool? playOnBuild = null;
        private static List<Service> services = null;
        private static ScriptingImplementation? scriptingImplementation = null;

        public static bool PlayOnBuild
        {
            get
            {
                if (playOnBuild == null)
                {
                    playOnBuild = EditorPrefs.GetBool(PlayerSettings.productGUID + PlayOnBuildKey, false);
                }

                return playOnBuild.Value;
            }
            set
            {
                if (playOnBuild != value)
                {
                    playOnBuild = value;
                    EditorPrefs.SetBool(PlayerSettings.productGUID + PlayOnBuildKey, value);
                }
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
                if (scriptingImplementation == null)
                {
                    scriptingImplementation = (ScriptingImplementation)EditorPrefs.GetInt(PlayerSettings.productGUID + BuildModeKey, 0);
                }

                return scriptingImplementation.Value;
            }
            set
            {
                if (scriptingImplementation != value)
                {
                    scriptingImplementation = value;
                    EditorPrefs.SetInt(PlayerSettings.productGUID + BuildModeKey, (int)value);
                }
            }
        }

        public static List<Service> Services
        {
            get
            {
                if (services == null)
                {
                    //load all services
                    services = new List<Service>();
                    var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                    foreach (var assembly in assemblies)
                    {
                        var types = assembly.GetTypes();
                        foreach (var type in types)
                        {
                            if (type.IsAbstract) continue;
                            if (type.IsSubclassOf(typeof(Service)))
                            {
                                Service instance = Activator.CreateInstance(type) as Service;
                                services.Add(instance);
                            }
                        }
                    }
                }

                return services;
            }
        }

        internal static List<(string text, MessageType type)> Log
        {
            get
            {
                const char Delimeter = (char)0;
                string value = EditorPrefs.GetString(LogKey);
                string[] array = value.Split('\n');
                List<(string text, MessageType type)> list = new List<(string text, MessageType type)>();
                for (int i = 0; i < array.Length; i++)
                {
                    if (array[i].Contains(Delimeter))
                    {
                        string text = array[i].Split(Delimeter)[0];
                        MessageType type = (MessageType)int.Parse(array[i].Split(Delimeter)[1]);
                        list.Add((text, type));
                    }
                }
                return list;
            }
            private set
            {
                const char Delimeter = (char)0;
                string[] array = new string[value.Count];
                for (int i = 0; i < value.Count; i++)
                {
                    array[i] = value[i].text + Delimeter + ((int)value[i].type);
                }

                string data = string.Join("\n", array);
                EditorPrefs.SetString(LogKey, data);
            }
        }

        internal static void Print(string text, MessageType type)
        {
            var list = Log;
            list.Insert(0, (text, type));

            if (list.Count > 10)
            {
                list.RemoveAt(list.Count - 1);
            }

            Log = list;
        }

        internal static void Clear()
        {
            var list = Log;
            list.Clear();
            Log = list;
        }

        internal static void Reset()
        {
            Clear();
            PlayOnBuild = false;
            Uploading = false;
            Building = false;
        }

        [MenuItem("Popcron/Builder/Build")]
        public static void Build()
        {
            Build("win");
        }

        [PostProcessBuild(1)]
        public static void OnPostprocessBuild(BuildTarget target, string path)
        {
            string platform = TargetToPlatform(target);
            string version = GetBuiltVersion(platform);

            string root = Directory.GetParent(Application.dataPath).FullName;
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

            string exportZip = path + ".zip";
            string archivedZip = root + "/Builds/" + platform + "/" + version + " (" + date + ").zip";

            if (File.Exists(exportZip))
            {
                File.Delete(exportZip);
            }
            if (!Directory.Exists(root + "/Builds"))
            {
                Directory.CreateDirectory(root + "/Builds");
            }
            if (!Directory.Exists(root + "/Builds/" + platform))
            {
                Directory.CreateDirectory(root + "/Builds/" + platform);
            }

            Print("Compressing " + path + " to " + exportZip, MessageType.Info);
            if (target == BuildTarget.WebGL)
            {
                Archiver.Zip(path + "/" + Settings.ExecutableName, exportZip, platform);
            }
            else
            {
                Archiver.Zip(path, exportZip, platform);
            }

            File.Copy(exportZip, archivedZip);
            EditorPrefs.SetString(Settings.GameName + "_builtArchive_" + platform, exportZip);
            Print("Exported archive to : " + exportZip, MessageType.Info);

            Building = false;
            OnPostBuild(platform);
        }

        public static BuildTarget PlatformToTarget(string platform)
        {
            if (platform == "win") return BuildTarget.StandaloneWindows64;
            if (platform == "linux") return BuildTarget.StandaloneLinuxUniversal;
            if (platform == "mac") return BuildTarget.StandaloneOSX;
            if (platform == "webgl") return BuildTarget.WebGL;

            throw new Exception(platform + " is not supported.");
        }

        public static string TargetToPlatform(BuildTarget target)
        {
            if (target == BuildTarget.StandaloneWindows64) return "win";
            if (target == BuildTarget.StandaloneOSX) return "mac";
            if (target == BuildTarget.StandaloneLinuxUniversal) return "linux";
            if (target == BuildTarget.WebGL) return "webgl";

            throw new Exception(target + " is not supported.");
        }

        public static string GetBuildPath(string platform)
        {
            string root = Directory.GetParent(Application.dataPath) + "/Game";
            if (platform == "win") return root + "/" + platform + "/" + Settings.ExecutableName + ".exe";
            if (platform == "mac") return root + "/" + platform + "/" + Settings.ExecutableName + ".app";
            if (platform == "linux") return root + "/" + platform + "/" + Settings.ExecutableName + ".x86";
            if (platform == "webgl") return root + "/" + platform;

            throw new Exception(platform + " is not supported.");
        }

        public static string GetBuiltPath(string platform)
        {
            return EditorPrefs.GetString(Settings.GameName + "_builtArchive_" + platform);
        }

        public static string GetPlayPath(string platform)
        {
            string root = Directory.GetParent(Application.dataPath) + "/Game/" + platform + "/";
            if (platform == "win") return root + Settings.ExecutableName + ".exe";
            if (platform == "mac") return root + Settings.ExecutableName + ".app";
            if (platform == "linux") return root + Settings.ExecutableName + ".x86";
            if (platform == "webgl") return root + "/index.html";

            throw new Exception(platform + " is not supported.");
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

            EditorPrefs.SetString(Settings.GameName + "_builtVersion_" + platform, Settings.CurrentVersion);

            Scene activeScene = SceneManager.GetActiveScene();
            string[] levels = new string[] { activeScene.path };

            if (!Directory.Exists(Directory.GetParent(Application.dataPath) + "/Game"))
            {
                Directory.CreateDirectory(Directory.GetParent(Application.dataPath) + "/Game");
            }

            //rebuild folder
            string folder = Directory.GetParent(Application.dataPath) + "/Game/" + platform;
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
            if (report.summary.result == BuildResult.Failed)
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
            return EditorPrefs.GetString(Settings.GameName + "_builtVersion_" + platform);
        }

        public static string GetUploadVersion(string platform)
        {
            return EditorPrefs.GetString(Settings.GameName + "_uploadVersion_" + platform);
        }

        public static async void Upload(string platform)
        {
            Uploading = true;
            string path = GetBuiltPath(platform);
            string version = GetBuiltVersion(platform);

            EditorPrefs.SetString(Settings.GameName + "_uploadedPlatform_", platform);
            EditorPrefs.SetString(Settings.GameName + "_uploadVersion_" + platform, version);

            //run through list of services
            //and upload to the ones that are allowed
            var tasks = new List<Task>();
            for (int i = 0; i < services.Count; i++)
            {
                if (services[i].CanUploadTo)
                {
                    Print("Started uploading to " + services[i].Name, MessageType.Info);
                    try
                    {
                        var task = services[i].Upload(path, version, platform);
                        tasks.Add(task);
                    }
                    catch (Exception exception)
                    {
                        Print(services[i].Name + ": " + exception.Message, MessageType.Error);
                    }
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