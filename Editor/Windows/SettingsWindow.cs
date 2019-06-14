using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Popcron.Builder
{
    public class SettingsWindow : EditorWindow
    {
        private const string ShowKey = "Popcron.Builder.ShowService";
		private double lastGitCheck = 0.0;
		private string gitExecutable = "default";

        [MenuItem("Popcron/Builder/Settings")]
        public static void Initialize()
        {
            SettingsWindow window = GetWindow<SettingsWindow>(false, "Settings");
        }

        private static List<T> Clone<T>(List<T> listToClone)
        {
            if (listToClone == null || listToClone.Count == 0) return new List<T>();

            var list = new List<T>();
            foreach (var item in listToClone)
            {
                list.Add(item);
            }
            return list;
        }

        //taken from https://stackoverflow.com/questions/12231569/is-there-in-c-sharp-a-method-for-listt-like-resize-in-c-for-vectort
        private static void Resize<T>(List<T> list, int size, T c = default)
        {
            int cur = list.Count;
            if (size < cur) list.RemoveRange(size, cur - size);
            else if (size > cur) list.AddRange(Enumerable.Repeat(c, size - cur));
        }

        private List<string> DrawArray(string title, List<string> list, ref bool show)
        {
            show = EditorGUILayout.Foldout(show, title);
            if (show)
            {
                EditorGUI.indentLevel++;
                List<string> clone = Clone(list);
                int size = clone.Count;
                bool changed = false;
                size = EditorGUILayout.IntField("Size", size);

                //sanitize the input
                if (size < 0) size = 0;
                if (size > 32) size = 32;

                //size of list changed
                if (size != list.Count)
                {
                    Resize(clone, size);
                    changed = true;
                }

                //draw the list
                for (int i = 0; i < clone.Count; i++)
                {
                    string directory = clone[i];
                    string newDirectory = EditorGUILayout.TextField(directory);
                    if (newDirectory != directory)
                    {
                        changed = true;
                        clone[i] = newDirectory;
                    }
                }

                if (changed)
                {
                    list = clone;
                }

                EditorGUI.indentLevel--;
            }

            return list;
        }

        private void DrawBlacklistedDirectories()
        {
            bool show = Settings.ShowBlacklistedDirectories;
            Settings.File.BlacklistedDirectories = DrawArray("Directories", Settings.File.BlacklistedDirectories, ref show);
            Settings.ShowBlacklistedDirectories = show;
        }

        private void DrawBlacklistedFiles()
        {
            bool show = Settings.ShowBlacklistedFiles;
            Settings.File.BlacklistedFiles = DrawArray("Files", Settings.File.BlacklistedFiles, ref show);
            Settings.ShowBlacklistedFiles = show;
        }

        private void OnGUI()
        {
            Repaint();
            Window.DrawHeader("Settings", "");

            float width = Mathf.Max(100f, Screen.width / 3f);
            if (GUI.Button(new Rect(Screen.width - width, 0, width, 17), "Reset", EditorStyles.toolbarButton))
            {
                Builder.Reset();
            }

            GUILayout.Space(20);

            //draw generic info
            Settings.File.GameName = EditorGUILayout.TextField("Game name", Settings.File.GameName);
            Settings.File.ExecutableName = EditorGUILayout.TextField("Executable name", Settings.File.ExecutableName);
            Settings.File.CurrentBuildDirectory = EditorGUILayout.TextField("Current build directory", Settings.File.CurrentBuildDirectory);
            Settings.File.BuildsDirectory = EditorGUILayout.TextField("Builds directory", Settings.File.BuildsDirectory);

            //show preview
            string buildFolder = Builder.GetBuildFolder(Builder.CurrentPlatform);
            string archivePath = Path.Combine(Path.GetFullPath(Settings.File.BuildsDirectory), Builder.CurrentPlatform);
            string[] lines = new string[]
            {
                "",
                "Game will be built as " + Settings.File.ExecutableName + ".exe",
                "To the \"" + buildFolder + "\" folder",
                "And also saved to \"" + archivePath + "\" folder as a .zip",
                ""
            };
            EditorGUILayout.HelpBox(string.Join("\n", lines), MessageType.Info);

            EditorGUILayout.LabelField("Blacklists", EditorStyles.boldLabel);
            {
                EditorGUI.indentLevel++;
                DrawBlacklistedDirectories();
                DrawBlacklistedFiles();
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.LabelField("General", EditorStyles.boldLabel);
            {
                EditorGUI.indentLevel++;
                Settings.File.UploadAfterBuild = EditorGUILayout.Toggle("Upload after building", Settings.File.UploadAfterBuild);

                Settings.File.BuildAfterGitPull = EditorGUILayout.Toggle("Build after pull", Settings.File.BuildAfterGitPull);
                if (Settings.File.BuildAfterGitPull)
				{
					int min = 30;
					int max = 60 * 10;
					Settings.File.GitFetchInterval = EditorGUILayout.IntSlider("Fetch interval (s)", Settings.File.GitFetchInterval, min, max);
					//Settings.File.GitPullMode = (PullMode)EditorGUILayout.EnumPopup("Pull mode", Settings.File.GitPullMode);
					if (EditorApplication.timeSinceStartup > lastGitCheck + 5)
					{
						lastGitCheck = EditorApplication.timeSinceStartup;
						gitExecutable = ProjectChangeChecker.GitExecutablePath();
					}
					
					if (string.IsNullOrEmpty(gitExecutable))
					{
						EditorGUILayout.HelpBox("Git executable not found in the environment table.", MessageType.Error);	
					}
					else
					{
						if (gitExecutable != "default")
						{
							EditorGUILayout.HelpBox("Will use " + gitExecutable + " to fetch and pull commits automatically.", MessageType.Info);	
						}
					}
				}
				
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.LabelField("Services", EditorStyles.boldLabel);
            {
                //draw services settings
                List<Service> services = Clone(Builder.Services);
                bool changed = false;

                EditorGUI.indentLevel++;

                //resize field
                int size = EditorGUILayout.IntField("Size", services.Count);
                if (size < 0) size = 0;
                if (size > 8) size = 8;
                if (size != services.Count)
                {
                    int diff = Mathf.Abs(services.Count - size);
                    if (size > services.Count)
                    {
                        //add more
                        for (int i = 0; i < diff; i++)
                        {
                            Service service = Service.Get("Empty", "Service");
                            services.Add(service);
                        }

                        changed = true;
                    }
                    else
                    {
                        //remove extra
                        for (int i = 0; i < diff; i++)
                        {
                            services.RemoveAt(services.Count - 1);
                        }

                        changed = true;
                    }
                }

                //services array
                for (int i = 0; i < services.Count; i++)
                {
                    services[i].Index = i;
                    bool show = EditorPrefs.GetBool(PlayerSettings.productGUID + ShowKey + "." + i, false);
                    show = EditorGUILayout.Foldout(show, services[i].Name);
                    if (show)
                    {
                        EditorGUI.indentLevel++;

                        //draw name field
                        string name = EditorGUILayout.TextField("Name", services[i].Name);
                        if (name != services[i].Name)
                        {
                            services[i].Name = name;
                            changed = true;
                        }

                        //draw type field
                        int selectedIndex = 0;
                        var uniqueServices = Service.Services;
                        string[] displayOptions = new string[uniqueServices.Count];
                        for (int s = 0; s < uniqueServices.Count; s++)
                        {
                            if (uniqueServices[s].typeName == services[i].Type)
                            {
                                selectedIndex = s;
                            }
                            displayOptions[s] = uniqueServices[s].typeName;
                        }
                        int newIndex = EditorGUILayout.Popup("Type", selectedIndex, displayOptions);
                        if (newIndex != selectedIndex)
                        {
                            services[i] = Service.Get(displayOptions[newIndex], name);
                            changed = true;
                        }

                        //draw service gui
                        services[i].OnGUI();

                        EditorGUI.indentLevel--;
                    }
                    EditorPrefs.SetBool(PlayerSettings.productGUID + ShowKey + "." + i, show);
                }

                if (changed)
                {
                    Builder.Services = services;
                    Builder.GetServices();
                }
            }

            EditorGUI.indentLevel--;
        }
    }
}