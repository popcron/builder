using System.Threading.Tasks;

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
