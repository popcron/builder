using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Popcron.Builder
{
    public abstract class Service
    {
        public abstract string Name { get; }
        public abstract bool CanUploadTo { get; set; }
        public abstract string URL { get; }

        public abstract Task Upload(string path, string version, string platform);
        public virtual bool OnGUI() { return false; }
    }
}
