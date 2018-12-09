using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

using Process = System.Diagnostics.Process;

namespace Popcron.Builder
{
    public class ItchService : Service
    {
        public const string ItchProjectName = "gun-game";          //taken from the edit game section when choosing a url
        public const string ItchAccount = "popcron";              //your itch profile name

        public static string ButlerPath
        {
            get
            {
                return Application.dataPath + "/Editor/butler.exe";
            }
        }

        public override string Name => "Itch";

        public override bool CanUploadTo
        {
            get
            {
                return EditorPrefs.GetBool(Name + "_" + ItchAccount + "_" + ItchProjectName, false);
            }
            set
            {
                EditorPrefs.SetBool(Name + "_" + ItchAccount + "_" + ItchProjectName, value);
            }
        }

        public override string URL => "https://" + ItchAccount + ".itch.io/" + ItchProjectName;

        public override async Task Upload(string path, string version, string platform)
        {
            if (!File.Exists(ButlerPath))
            {
                //The bulter.exe doesnt exist, create it and redo it.
                Debug.LogError("butler.exe doesnt exist in the Editor folder, download at https://dl.itch.ovh/butler/windows-amd64/head/butler.exe");
                return;
            }

            Process itchButler = new Process();
            itchButler.StartInfo.FileName = ButlerPath;
            itchButler.StartInfo.Arguments = "push \"" + path + "\" " + ItchAccount + "/" + ItchProjectName + ":" + platform + " --userversion " + version;
            itchButler.Start();

            while (!itchButler.HasExited)
            {
                await Task.Delay(10);
            }

            //done
        }
    }
}
