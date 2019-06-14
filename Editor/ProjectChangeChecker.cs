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
			if (Settings.File.BuildAfterGitPull)
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
					string fileName = "cmd.exe";
					int platform = (int)System.Environment.OSVersion.Platform;
					if (platform == 4 || platform == 6 || platform == 128)
					{
						fileName = "/bin/bash";
					}
					
					ProcessStartInfo info = new ProcessStartInfo
					{
						FileName = fileName,
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

					//Debug.Log("finished pulling");
				}
				else
				{
					Debug.LogError("Could't find a .git folder. Is this an actual repository?.");
				}
			}
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