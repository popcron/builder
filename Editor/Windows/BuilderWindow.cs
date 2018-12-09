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
            Window.DrawHeader("", "");
            DrawVersionInformation();

            //loop through services, and display their gui
            bool consumedEvent = false;
            for (int i = 0; i < Builder.Services.Count; i++)
            {
                consumedEvent |= Builder.Services[i].OnGUI();
            }

            //dont draw the builder ui if the ongui event was consumed
            if (consumedEvent)
            {
                return;
            }

            if (Builder.Building)
            {
                EditorGUILayout.LabelField("Building...", EditorStyles.boldLabel);
                return;
            }

            Event e = Event.current;

            //loop through the services, and toggle each setting
            int uploadServices = 0;
            for (int i = 0; i < Builder.Services.Count; i++)
            {
                Builder.Services[i].CanUploadTo = EditorGUILayout.Toggle("Upload to " + Builder.Services[i].Name, Builder.Services[i].CanUploadTo);
                if (Builder.Services[i].CanUploadTo)
                {
                    uploadServices++;
                }
            }

            //mode enum
            Builder.ScriptingImplementation = (ScriptingImplementation)EditorGUILayout.EnumPopup("Mode", Builder.ScriptingImplementation);

            Rect lastRect = GUILayoutUtility.GetLastRect();

            //toolbar garbage
            string platform = EditorPrefs.GetString(Settings.GameName + "_buildPlatform", "win");
            string[] platforms = new string[] { "win", "linux", "mac", "webgl" };
            int currentIndex = IndexOf(platform, platforms);

            platform = platforms[GUI.Toolbar(new Rect(0, lastRect.y + lastRect.height + 5, Screen.width, 20), currentIndex, platforms, EditorStyles.toolbarButton)];
            EditorPrefs.SetString(Settings.GameName + "_buildPlatform", platform);

            GUILayout.Space(25);

            //upload button
            if (Builder.UploadExists(platform) && uploadServices > 0)
            {
                if (GUILayout.Button("Upload", EditorStyles.miniButton, GUILayout.Height(20)))
                {
                    Builder.Upload(platform);
                }
            }
            else
            {
                GUI.color = new Color(1f, 1f, 1f, 0.3f);

                GUILayout.Button("Upload", EditorStyles.miniButton, GUILayout.Height(20));

                GUI.color = Color.white;
            }

            //build and play button
            GUILayout.BeginHorizontal();
            if (!Builder.Building)
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

            lastRect = GUILayoutUtility.GetLastRect();

            //draw the urls for each service
            for (int i = 0; i < Builder.Services.Count; i++)
            {
                if (GUI.Button(new Rect(0, lastRect.y + lastRect.height + 8 + i * 20, Screen.width, 20), "go to " + Builder.Services[i].Name, EditorStyles.toolbarButton))
                {
                    Application.OpenURL(Builder.Services[i].URL);
                }
            }
        }
    }
}