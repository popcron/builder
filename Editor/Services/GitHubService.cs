using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using System.IO;

using Octokit;

namespace Popcron.Builder
{
    public class GitHubService : Service
    {
        //github repo location
        public const string GitHubOwner = "popcron";
        public const string GitHubRepo = "gun-game";

        private bool login;

        private static string GitHubToken
        {
            get
            {
                return EditorPrefs.GetString(GitHubOwner + ":" + GitHubRepo + "_githubToken", "");
            }
            set
            {
                EditorPrefs.SetString(GitHubOwner + ":" + GitHubRepo + "_githubToken", value);
            }
        }

        public override string Name => "GitHub";

        public override string URL => "https://github.com/" + GitHubOwner + "/" + GitHubRepo;

        public override bool CanUploadTo
        {
            get
            {
                return EditorPrefs.GetBool(Name + "_" + GitHubOwner + "_" + GitHubRepo, false);
            }
            set
            {
                EditorPrefs.SetBool(Name + "_" + GitHubOwner + "_" + GitHubRepo, value);
            }
        }

        public override async Task Upload(string path, string version, string platform)
        {
            if (GitHubToken == "")
            {
                login = true;

                //wait until the user logs in
                while (login)
                {
                    await Task.Delay(10);
                }
            }

            string releaseName = "version " + version;

            GitHubClient client = new GitHubClient(new ProductHeaderValue(GitHubRepo))
            {
                Credentials = new Credentials(GitHubToken)
            };

            Repository repo = await client.Repository.Get(GitHubOwner, GitHubRepo);

            //check if a release with this version already exists
            bool exists = false;
            IReadOnlyList<Release> releases = await client.Repository.Release.GetAll(GitHubOwner, GitHubRepo);
            foreach (var release in releases)
            {
                if (release.Name == releaseName)
                {
                    //already exists
                    exists = true;
                    break;
                }
            }

            if (!exists)
            {
                //create release
                NewRelease newRelease = new NewRelease(version)
                {
                    Name = releaseName,
                    Body = "",
                    Draft = false,
                    Prerelease = false
                };

                Release release = await client.Repository.Release.Create(GitHubOwner, GitHubRepo, newRelease);
                Debug.Log("Created release with ID " + release.Id);

                //update the release with a file
                using (var archiveContents = File.OpenRead(path))
                {
                    var assetUpload = new ReleaseAssetUpload()
                    {
                        FileName = Path.GetFileName(path),
                        ContentType = "application/zip",
                        RawData = archiveContents
                    };

                    var asset = await client.Repository.Release.UploadAsset(release, assetUpload);
                    Debug.Log("Uploaded " + path + " to new release (" + release.Url + ")");
                }
            }
            else
            {
                Debug.LogError("Release with version " + version + " already exists");
            }
        }

        public override bool OnGUI()
        {
            if (login)
            {
                GitHubToken = EditorGUILayout.TextField("Token", GitHubToken);

                if (GUILayout.Button("Set"))
                {
                    login = false;
                }
                return true;
            }

            return false;
        }
    }
}
