using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Popcron.Builder
{
    public enum PullMode
    {
        Next,
        Latest
    }

    [Serializable]
    public class SettingsFile
    {
        private string path;

        [SerializeField]
        private string currentBuildDirectory;

        [SerializeField]
        private string buildsDirectory;

        [SerializeField]
        private string gameName;

        [SerializeField]
        private string executableName;

        [SerializeField]
        private List<string> blacklistedDirectories = new List<string>();

        [SerializeField]
        private List<string> blacklistedFiles = new List<string>();

        public string CurrentBuildDirectory
        {
            get
            {
                return currentBuildDirectory;
            }
            set
            {
                currentBuildDirectory = value;
                Save();
            }
        }

        public string BuildsDirectory
        {
            get
            {
                return buildsDirectory;
            }
            set
            {
                buildsDirectory = value;
                Save();
            }
        }

        public string GameName
        {
            get
            {
                return gameName;
            }
            set
            {
                gameName = value;
                Save();
            }
        }

        public string ExecutableName
        {
            get
            {
                return executableName;
            }
            set
            {
                executableName = value;
                Save();
            }
        }

        public List<string> BlacklistedDirectories
        {
            get
            {
                return blacklistedDirectories;
            }
            set
            {
                blacklistedDirectories = value;
                Save();
            }
        }

        public List<string> BlacklistedFiles
        {
            get
            {
                return blacklistedFiles;
            }
            set
            {
                blacklistedFiles = value;
                Save();
            }
        }

        public bool UploadAfterBuild
        {
            get
            {
                bool uploadAfterBuild = EditorPrefs.GetBool(PlayerSettings.productGUID + "_uploadAfterBuild", false);
                return uploadAfterBuild;
            }
            set
            {
                EditorPrefs.SetBool(PlayerSettings.productGUID + "_uploadAfterBuild", value);
            }
        }

        public int GitFetchInterval
        {
            get
            {
                int gitFetchInterval = EditorPrefs.GetInt(PlayerSettings.productGUID + "_gitFetchInterval", 30);
                return gitFetchInterval;
            }
            set
            {
                EditorPrefs.SetInt(PlayerSettings.productGUID + "_gitFetchInterval", value);
            }
        }

        public bool BuildAfterGitPull
        {
            get
            {
                bool autoBuildOnChange = EditorPrefs.GetBool(PlayerSettings.productGUID + "_buildAfterGitPull", false);
                return autoBuildOnChange;
            }
            set
            {
                EditorPrefs.SetBool(PlayerSettings.productGUID + "_buildAfterGitPull", value);
            }
        }

        public PullMode GitPullMode
        {
            get
            {
                int gitPullMode = EditorPrefs.GetInt(PlayerSettings.productGUID + "_gitPullMode", 0);
                return (PullMode)gitPullMode;
            }
            set
            {
                EditorPrefs.SetInt(PlayerSettings.productGUID + "_gitPullMode", (int)value);
            }
        }

        public static SettingsFile Create(string path)
        {
            SettingsFile file = new SettingsFile(path);
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                JsonUtility.FromJsonOverwrite(json, file);
            }
            else
            {
                string productName = PlayerSettings.productName;
                string projectPath = Directory.GetParent(Application.dataPath).FullName;

                string buildsFolder = Path.Combine(projectPath, "Builds");
                string gameFolder = Path.Combine(projectPath, "Game");

                file.buildsDirectory = buildsFolder;
                file.currentBuildDirectory = gameFolder;
                file.gameName = productName;
                file.executableName = productName;

                file.blacklistedDirectories = new List<string>();
                file.blacklistedDirectories.Add(productName + "_BackUpThisFolder_ButDontShipItWithYourGame");

                file.blacklistedFiles = new List<string>();
                file.blacklistedFiles.Add("WiPixEventRuntime.dll");

                file.Save();
            }

            return file;
        }

        private void Save()
        {
            string json = JsonUtility.ToJson(this, true);
            File.WriteAllText(path, json);
        }

        private SettingsFile(string path)
        {
            this.path = path;
        }
    }
}