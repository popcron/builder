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
    public class EmptyService : Service
    {
        public override string Type => "Empty";

        public override bool CanUploadTo
        {
            get
            {
                return false;
            }
            set
            {

            }
        }

        public override string URL => "";

        
        public override async Task Upload(string path, string version, string platform)
        {
            await Task.CompletedTask;
            return;
        }

        public override void OnGUI()
        {

        }
    }
}
