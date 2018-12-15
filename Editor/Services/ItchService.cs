using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

using Process = System.Diagnostics.Process;

namespace Popcron.Builder
{
    public class ItchService : Service
    {
        private const string ItchProjectNameKey = "Popcron.Builder.ItchService.ItchProjectName";
        private const string ItchAccountKey = "Popcron.Builder.ItchService.ItchAccount";
        private const string ButlerPathKey = "Popcron.Builder.ItchService.ButlerPath";
        private const string DownloadProgressKey = "Popcron.Builder.DownloadingProgress";

        public static string ItchProjectName
        {
            get
            {
                return EditorPrefs.GetString(PlayerSettings.productGUID + ItchProjectNameKey, PlayerSettings.productName);
            }
            set
            {
                if (value == null)
                {
                    EditorPrefs.DeleteKey(PlayerSettings.productGUID + ItchProjectNameKey);
                }
                else
                {
                    EditorPrefs.SetString(PlayerSettings.productGUID + ItchProjectNameKey, value);
                }
            }
        }

        public static string ItchAccount
        {
            get
            {
                return EditorPrefs.GetString(PlayerSettings.productGUID + ItchAccountKey, PlayerSettings.companyName);
            }
            set
            {
                if (value == null)
                {
                    EditorPrefs.DeleteKey(PlayerSettings.productGUID + ItchAccountKey);
                }
                else
                {
                    EditorPrefs.SetString(PlayerSettings.productGUID + ItchAccountKey, value);
                }
            }
        }

        public static string ButlerDirectory
        {
            get
            {
                string path = EditorPrefs.GetString(PlayerSettings.productGUID + ButlerPathKey);
                if (string.IsNullOrEmpty(path))
                {
                    string roamingPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\itch\\broth\\butler";
                    string chosenVersionFile = roamingPath + "\\.chosen-version";
                    string chosenVersion = null;
                    if (File.Exists(chosenVersionFile))
                    {
                        chosenVersion = File.ReadAllLines(chosenVersionFile)[0];
                    }

                    if (chosenVersion != null)
                    {
                        return roamingPath + "\\versions\\" + chosenVersion;
                    }
                }

                return path;
            }
            set
            {
                if (value == null)
                {
                    EditorPrefs.DeleteKey(PlayerSettings.productGUID + ButlerPathKey);
                }
                else
                {
                    EditorPrefs.SetString(PlayerSettings.productGUID + ButlerPathKey, value);
                }
            }
        }

        private static float? DownloadProgress
        {
            get
            {
                float value = EditorPrefs.GetFloat(PlayerSettings.productGUID + DownloadProgressKey, -1);
                return value >= 0 ? value : (float?)null;
            }
            set
            {
                if (value != null && value >= 0)
                {
                    EditorPrefs.SetFloat(PlayerSettings.productGUID + DownloadProgressKey, value.Value);
                }
                else
                {
                    EditorPrefs.DeleteKey(PlayerSettings.productGUID + DownloadProgressKey);
                }
            }
        }

        public override string Name => "Itch";

        public override bool CanUploadTo
        {
            get
            {
                return EditorPrefs.GetBool(PlayerSettings.productGUID + Name + "_" + ItchAccount + "_" + ItchProjectName, false);
            }
            set
            {
                EditorPrefs.SetBool(PlayerSettings.productGUID + Name + "_" + ItchAccount + "_" + ItchProjectName, value);
            }
        }

        public override string URL => "https://" + ItchAccount + ".itch.io/" + ItchProjectName;

        private async void Download()
        {
            Directory.CreateDirectory(ButlerDirectory);

            DownloadProgress = 0;
            string url = "https://dl.itch.ovh/butler/windows-amd64/head/butler.exe";
            WebClient webClient = new WebClient();
            webClient.DownloadProgressChanged += DownloadProgressChanged;
            await webClient.DownloadFileTaskAsync(url, ButlerDirectory + "/butler.exe");
            DownloadProgress = -1;

            AssetDatabase.Refresh();
        }

        private void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            DownloadProgress = e.ProgressPercentage / 100f;
        }

        public override async Task Upload(string path, string version, string platform)
        {
            string butlerPath = ButlerDirectory + "/butler.exe";
            if (!File.Exists(butlerPath))
            {
                //The bulter.exe doesnt exist, create it and redo it.
                Builder.Print("Itch: butler.exe wasnt found. Check the settings and make sure that the executable eixsts", MessageType.Error);
                return;
            }

            Process itchButler = new Process();
            itchButler.StartInfo.FileName = butlerPath;
            itchButler.StartInfo.Arguments = "push \"" + path + "\" " + ItchAccount + "/" + ItchProjectName + ":" + platform + " --userversion " + version;
            itchButler.Start();

            while (!itchButler.HasExited)
            {
                await Task.Delay(10);
            }

            Builder.Print("Itch: Finished", MessageType.Info);
        }

        public override void OnGUI()
        {
            ItchAccount = EditorGUILayout.TextField("Account", ItchAccount);
            ItchProjectName = EditorGUILayout.TextField("Name", ItchProjectName);
            ButlerDirectory = EditorGUILayout.TextField("Butler path", ButlerDirectory);

            if (GUILayout.Button("Clear path"))
            {
                ButlerDirectory = null;
            }

            if (DownloadProgress != null || !File.Exists(ButlerDirectory + "/butler.exe"))
            {
                if (DownloadProgress != null)
                {
                    string percentage = Mathf.RoundToInt(DownloadProgress.Value * 100) + "%";
                    EditorGUILayout.HelpBox("Downloading... " + percentage, MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox("butler.exe not found in " + ButlerDirectory, MessageType.Error);
                    if (GUILayout.Button("Download butler.exe"))
                    {
                        Download();
                    }
                }
            }
        }
    }
}
