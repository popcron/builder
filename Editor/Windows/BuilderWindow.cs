using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Popcron.Builder
{
    public class BuilderWindow : EditorWindow
    {
        [MenuItem("Popcron/Builder/Builder")]
        public static void Initialize()
        {
            BuilderWindow window = GetWindow<BuilderWindow>(false, "Builder");
        }

        private int IndexOf(string predicate, string[] options)
        {
            for (int i = 0; i < options.Length; i++)
            {
                if (options[i] == predicate) return i;
            }

            return -1;
        }

        private void DrawVersionInformation()
        {
            GUI.BeginGroup(new Rect(0, 0, Screen.width, 50));
            GUILayout.BeginHorizontal();

            //header info
            GUILayout.Label("Current: " + Settings.CurrentVersion, EditorStyles.miniLabel);
            GUILayout.Label("Last: " + Settings.LastVersion, EditorStyles.miniLabel);

            GUILayout.EndHorizontal();
            GUI.EndGroup();
        }

        private void OnGUI()
        {
            Repaint();
            Window.DrawHeader("", "");
            DrawVersionInformation();

            if (Builder.Building)
            {
                if (GUILayout.Button("Cancel"))
                {
                    Builder.Building = false;
                }

                EditorGUILayout.LabelField("Building...", EditorStyles.boldLabel);
                return;
            }

            Event e = Event.current;

            //loop through the services, and toggle each setting
            int uploadServices = 0;
            List<Service> services = Builder.Services;
            for (int i = 0; i < services.Count; i++)
            {
                services[i].Index = i;
                services[i].CanUploadTo = EditorGUILayout.Toggle("Upload to " + services[i].Name, services[i].CanUploadTo);
                if (services[i].CanUploadTo)
                {
                    uploadServices++;
                }
            }

            //mode enum
            Builder.ScriptingImplementation = (ScriptingImplementation)EditorGUILayout.EnumPopup("Mode", Builder.ScriptingImplementation);

            //development stuff
            Builder.ProfilerDebug = EditorGUILayout.Toggle("Autoconnect Profiler", Builder.ProfilerDebug);

            Rect lastRect = GUILayoutUtility.GetLastRect();

            //toolbar garbage
            string platform = EditorPrefs.GetString(Settings.GameName + "_buildPlatform", "win");
            string[] platforms = new string[] { "win", "linux", "mac", "webgl" };
            int currentIndex = IndexOf(platform, platforms);

            platform = platforms[GUI.Toolbar(new Rect(0, lastRect.y + lastRect.height + 5, Screen.width, 20), currentIndex, platforms, EditorStyles.toolbarButton)];
            EditorPrefs.SetString(Settings.GameName + "_buildPlatform", platform);

            GUILayout.Space(25);

            //upload button
            if (Builder.UploadExists(platform) && uploadServices > 0 && !Builder.Uploading)
            {
                if (GUILayout.Button("Upload", GUILayout.Height(40)))
                {
                    Builder.Upload(platform);
                }
            }
            else
            {
                GUI.color = new Color(1f, 1f, 1f, 0.3f);

                GUILayout.Button("Upload", GUILayout.Height(40));

                GUI.color = Color.white;
            }

            //build and play button
            GUILayout.BeginHorizontal();
            if (Builder.Building)
            {
                GUI.color = new Color(1f, 1f, 1f, 0.3f);

                GUILayout.Button("Build", EditorStyles.miniButtonLeft, GUILayout.Height(20));
                GUILayout.Button("Build + Play", EditorStyles.miniButtonMid, GUILayout.Height(20));

                GUI.color = Color.white;
            }
            else
            {
                if (GUILayout.Button("Build", EditorStyles.miniButtonLeft, GUILayout.Height(20)))
                {
                    Builder.Build(platform);
                }
                if (GUILayout.Button("Build + Play", EditorStyles.miniButtonMid, GUILayout.Height(20)))
                {
                    Builder.BuildAndPlay(platform);
                }
            }
            if (Builder.PlayExists(platform))
            {
                if (GUILayout.Button("Play", EditorStyles.miniButtonRight, GUILayout.Height(20)))
                {
                    Builder.Play(platform);
                }
            }
            else
            {
                GUI.color = new Color(1f, 1f, 1f, 0.3f);

                GUILayout.Button("Play", EditorStyles.miniButtonRight, GUILayout.Height(20));

                GUI.color = Color.white;
            }
            GUILayout.EndHorizontal();

            //draw the message window
            GUILayout.Space(50);
            var rect = GUILayoutUtility.GetLastRect();
            Window.DrawHeader("Log", "", rect.yMax);

            float width = Mathf.Max(60f, Screen.width / 3f);
            if (GUI.Button(new Rect(Screen.width - width, rect.yMax, width, 17), "Clear", EditorStyles.toolbarButton))
            {
                Builder.ClearLog();
            }

            GUILayout.Space(20);
            foreach (var (text, type) in Builder.Log)
            {
                EditorGUILayout.HelpBox(text, type);
            }
        }
    }
}