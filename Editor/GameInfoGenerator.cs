using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace Popcron.Builder
{
    public class GameInfoGenerator
    {
        public static void CompileGameInfo(string platform, string version)
        {
            string name = Settings.File.GameName;
            string filePath = Path.Combine(Application.dataPath, "GameInfo.cs");
            string[] fileContents = new string[]
            {
                "namespace Popcron.Builder",
                "{",
                "\tpublic static class GameInfo",
                "\t{",
                "\t\tpublic const string Name = \"" + name + "\";",
                "\t\tpublic const string Platform = \"" + platform + "\";",
                "\t\tpublic const string Version = \"" + version + "\";",
                "\t\tpublic const string Implementation = \"" + Builder.ScriptingImplementation.ToString() + "\";",
                "\t}",
                "}"
            };

            File.WriteAllLines(filePath, fileContents);
            AssetDatabase.Refresh();
        }
    }
}
