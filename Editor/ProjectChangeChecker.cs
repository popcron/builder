using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Popcron.Builder
{
    [InitializeOnLoad]
    public class ProjectChangeChecker
    {
        static ProjectChangeChecker()
        {
            EditorApplication.update += OnUpdate;
        }

        private static void OnUpdate()
        {
            float interval = Settings.File.GitFetchInterval;
            float lastGitFetch = EditorPrefs.GetFloat(PlayerSettings.productGUID + "_lastGitFetch", 0f);
            if (EditorApplication.timeSinceStartup > lastGitFetch + interval)
            {
                lastGitFetch = (float)EditorApplication.timeSinceStartup;
                EditorPrefs.SetFloat(PlayerSettings.productGUID + "_lastGitFetch", lastGitFetch);

                GitFetch();
            }
        }

        /// <summary>
        /// Checks every now and then for a commit change
        /// </summary>
        private static void GitFetch()
        {
            string gitExecutable = GitExecutablePath();
            if (!string.IsNullOrEmpty(gitExecutable))
            {
                string path = Directory.GetParent(Application.dataPath).FullName;
                if (!Directory.Exists(Path.Combine(path, ".git")))
                {
                    path = Directory.GetParent(path).FullName;
                }
                if (!Directory.Exists(Path.Combine(path, ".git")))
                {
                    path = Directory.GetParent(path).FullName;
                }

                if (Directory.Exists(Path.Combine(path, ".git")))
                {
                    ProcessStartInfo info = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        RedirectStandardInput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    Process process = Process.Start(info);
                    using (StreamWriter writer = process.StandardInput)
                    {
                        writer.WriteLine("cd \"" + path + "\"");
                        writer.WriteLine("git fetch");
                        writer.WriteLine("git pull");
                    }

                    Debug.Log("finished pulling");
                }
                else
                {
                    Debug.LogError("Could't find a .git folder. Is this an actual repository?.");
                }
            }
            else
            {
                Debug.LogError("Couldn't find git.exe. Make sure that a path to its directory exists in the PATH environment variable.");
            }
        }

        private static string GitExecutablePath()
        {
            IDictionary variables = Environment.GetEnvironmentVariables();
            foreach (string item in variables.Keys)
            {
                string value = Environment.GetEnvironmentVariable(item);
                if (item.ToLower() == "path")
                {
                    string[] paths = value.Split(';');
                    for (int i = 0; i < paths.Length; i++)
                    {
                        string gitExe = Path.Combine(paths[i], "git.exe");
                        if (File.Exists(gitExe))
                        {
                            return gitExe;
                        }
                    }
                }
            }

            return null;
        }

        [PostProcessBuild(2)]
        public static void OnPostprocessBuild(BuildTarget target, string path)
        {
            if (Settings.File.UploadAfterBuild)
            {
                int uploadServices = 0;
                List<Service> services = Builder.Services;
                for (int i = 0; i < services.Count; i++)
                {
                    if (services[i].CanUploadTo)
                    {
                        uploadServices++;
                    }
                }

                string platform = Builder.TargetToPlatform(target);
                if (Builder.UploadExists(platform) && uploadServices > 0 && !Builder.Uploading)
                {
                    Builder.Upload(platform);
                }
            }
        }
    }
}