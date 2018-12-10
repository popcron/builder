using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace Popcron.Builder
{
    public class SettingsWindow : EditorWindow
    {
        [MenuItem("Popcron/Builder/Settings")]
        public static void Initialize()
        {
            SettingsWindow window = GetWindow<SettingsWindow>(false, "Settings");
        }

        private static List<T> Clone<T>(List<T> listToClone) where T : ICloneable
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
        private static void Resize<T>(List<T> list, int size, T c = default(T))
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
                var directories = list;
                var clone = Clone(directories);
                int count = clone.Count;
                bool changed = false;
                count = EditorGUILayout.IntField("Size", count);

                //sanitize the input
                if (count < 0) count = 0;
                if (count > 32) count = 32;

                //size of list changed
                if (count != directories.Count)
                {
                    Resize(clone, count);
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
            Settings.BlacklistedDirectories = DrawArray("Directories", Settings.BlacklistedDirectories, ref show);
            Settings.ShowBlacklistedDirectories = show;
        }

        private void DrawBlacklistedFiles()
        {
            bool show = Settings.ShowBlacklistedFiles;
            Settings.BlacklistedFiles = DrawArray("Files", Settings.BlacklistedFiles, ref show);
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
            Settings.GameName = EditorGUILayout.TextField("Game name", Settings.GameName);
            Settings.ExecutableName = EditorGUILayout.TextField("Executable name", Settings.ExecutableName);

            EditorGUILayout.HelpBox("Game will be built as " + Settings.ExecutableName + ".exe", MessageType.Info);

            EditorGUILayout.LabelField("Blacklists", EditorStyles.boldLabel);

            DrawBlacklistedDirectories();
            DrawBlacklistedFiles();

            EditorGUILayout.LabelField("Services", EditorStyles.boldLabel);

            //draw services settings
            foreach (var service in Builder.Services)
            {
                service.Show = EditorGUILayout.Foldout(service.Show, service.Name);
                if (service.Show)
                {
                    EditorGUI.indentLevel++;
                    service.OnGUI();
                    EditorGUI.indentLevel--;
                }
            }
        }
    }
}