using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Popcron.Builder
{
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
        private List<string> blacklistedDirectories;

        [SerializeField]
        private List<string> blacklistedFiles;

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
                blacklistedDirectories = value;
                Save();
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
                file.blacklistedFiles = new List<string>();

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