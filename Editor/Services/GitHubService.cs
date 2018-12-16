using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using System.IO;

using System.Net.Http;
using System.Threading;
using System.Net;

namespace Popcron.Builder
{
    public class GitHubService : Service
    {
        [Serializable]
        public class CreateRequest
        {
            public string tag_name = "";
            public string target_commitish = "";
            public string name = "";
            public string body = "";
            public bool draft = false;
            public bool prerelease = false;
        }

        [Serializable]
        public class ReleasesResponse
        {
            public List<Release> releases = new List<Release>();
        }

        [Serializable]
        public class Release
        {
            public string url = "";
            public string assets_url = "";
            public string upload_url = "";
            public string html_url = "";
            public string id = "";
            public string node_id = "";
            public string tag_name = "";
            public string target_commitish = "";
            public string name = "";
            public bool draft = false;
            public bool prerelease = false;
            public string created_at = "";
            public string published_at = "";
        }

        private const string PrefixKey = "Popcron.Builder.GitHubService.Prefix";
        private const string OwnerKey = "Popcron.Builder.GitHubService.Owner";
        private const string RepositoryKey = "Popcron.Builder.GitHubService.Repository";
        private const string TokenKey = "Popcron.Builder.GitHubService.Token";
        private const string ShowTokenKey = "Popcron.Builder.GitHubService.ShowToken";
        private const string UserAgent = "Popcron.Builder.GitHubService";

        public string Prefix
        {
            get
            {
                return EditorPrefs.GetString(PlayerSettings.productGUID + PrefixKey + Index, "v");
            }
            set
            {
                EditorPrefs.SetString(PlayerSettings.productGUID + PrefixKey + Index, value);
            }
        }

        public string Owner
        {
            get
            {
                return EditorPrefs.GetString(PlayerSettings.productGUID + OwnerKey + Index, PlayerSettings.companyName);
            }
            set
            {
                EditorPrefs.SetString(PlayerSettings.productGUID + OwnerKey + Index, value);
            }
        }

        public string Repository
        {
            get
            {
                return EditorPrefs.GetString(PlayerSettings.productGUID + RepositoryKey + Index, PlayerSettings.productName);
            }
            set
            {
                EditorPrefs.SetString(PlayerSettings.productGUID + RepositoryKey + Index, value);
            }
        }

        public string Token
        {
            get
            {
                return EditorPrefs.GetString(PlayerSettings.productGUID + TokenKey + Index);
            }
            set
            {
                EditorPrefs.SetString(PlayerSettings.productGUID + TokenKey + Index, value);
            }
        }

        public bool ShowToken
        {
            get
            {
                return EditorPrefs.GetBool(PlayerSettings.productGUID + ShowTokenKey + Index);
            }
            set
            {
                EditorPrefs.SetBool(PlayerSettings.productGUID + ShowTokenKey + Index, value);
            }
        }

        public override string Type => "GitHub";

        public override string URL => "https://github.com/" + Owner + "/" + Repository;

        public override bool CanUploadTo
        {
            get
            {
                return EditorPrefs.GetBool(Type + "_" + Owner + "_" + Repository + Index, false);
            }
            set
            {
                EditorPrefs.SetBool(Type + "_" + Owner + "_" + Repository + Index, value);
            }
        }

        public override async Task Upload(string path, string version, string platform)
        {
            if (string.IsNullOrEmpty(Token))
            {
                Builder.Print(Name + ": No access token.", MessageType.Error);
                return;
            }

            string releaseName = Settings.GameName + " " + version;
            string tag = Prefix + version;

            string auth = "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(Owner + ":" + Token));
            bool releaseAlreadyExists = false;

            using (var httpClient = new HttpClient())
            {
                using (var request = new HttpRequestMessage(new HttpMethod("GET"), "https://api.github.com/repos/" + Owner + "/" + Repository + "/releases"))
                {
                    request.Headers.TryAddWithoutValidation("User-Agent", UserAgent);
                    request.Headers.TryAddWithoutValidation("Authorization", auth);

                    var response = await httpClient.SendAsync(request);
                    using (var reader = new StreamReader(await response.Content.ReadAsStreamAsync()))
                    {
                        string data = await reader.ReadToEndAsync();
                        if (data.Contains("\"message\":\"Not Found\""))
                        {
                            Builder.Print(Name + ":  " + Owner + "/" + Repository + " repository not found.", MessageType.Error);
                            return;
                        }

                        data = "{\"releases\":" + data + "}";

                        var releases = JsonUtility.FromJson<ReleasesResponse>(data);
                        foreach (var release in releases.releases)
                        {
                            if (release.name == releaseName)
                            {
                                releaseAlreadyExists = true;
                                break;
                            }
                        }
                    }
                }
            }

            if (!releaseAlreadyExists)
            {
                Builder.Print(Name + ": Creating new release...", MessageType.Info);
                var createRequest = new CreateRequest
                {
                    name = releaseName,
                    tag_name = tag
                };

                string postRequest = JsonUtility.ToJson(createRequest);
                int id = 0;
                using (var httpClient = new HttpClient())
                {
                    using (var request = new HttpRequestMessage(new HttpMethod("POST"), "https://api.github.com/repos/" + Owner + "/" + Repository + "/releases"))
                    {
                        request.Headers.TryAddWithoutValidation("User-Agent", UserAgent);
                        request.Headers.TryAddWithoutValidation("Authorization", auth);

                        request.Content = new StringContent(postRequest, Encoding.UTF8, "application/json");

                        var response = await httpClient.SendAsync(request);
                        using (var reader = new StreamReader(await response.Content.ReadAsStreamAsync()))
                        {
                            string data = await reader.ReadToEndAsync();
                            if (data.Contains("Validation Failed") && data.Contains("already_exists") && data.Contains("tag_name"))
                            {
                                Builder.Print(Name + ": Release with tag " + tag + " already exists", MessageType.Error);
                                return;
                            }

                            string scrapedId = data.Substring(data.IndexOf("id") + 4);
                            if (int.TryParse(scrapedId.Substring(0, scrapedId.IndexOf(",")), out int result))
                            {
                                id = result;
                                Builder.Print(Name + ": Created release with ID " + id + ".", MessageType.Info);
                            }
                        }
                    }
                }

                //upload asset to release
                string archiveName = Path.GetFileName(path);
                Builder.Print(Name + ": Uploading " + archiveName + "", MessageType.Info);

                byte[] archiveData = File.ReadAllBytes(path);
                string url = "https://uploads.github.com/repos/" + Owner + "/" + Repository + "/releases/" + id + "/assets?name=" + archiveName;

                HttpWebRequest uploadRequest = (HttpWebRequest)WebRequest.Create(url);
                uploadRequest.Method = "POST";
                uploadRequest.UserAgent = UserAgent;
                uploadRequest.Headers.Add("Authorization", auth);
                uploadRequest.ContentType = "application/zip";
                uploadRequest.ContentLength = archiveData.Length;

                //write content data
                using (Stream stream = await uploadRequest.GetRequestStreamAsync())
                {
                    await stream.WriteAsync(archiveData, 0, archiveData.Length);
                }

                await uploadRequest.GetResponseAsync();
                Builder.Print(Name + ": Finished.", MessageType.Info);
            }
            else
            {
                Builder.Print(Name + ": Release with name " + releaseName + " already exists.", MessageType.Error);
            }
        }

        public override void OnGUI()
        {
            Owner = EditorGUILayout.TextField("Owner", Owner);
            Repository = EditorGUILayout.TextField("Repository", Repository);
            Prefix = EditorGUILayout.TextField("Prefix", Prefix);

            ShowToken = EditorGUILayout.Foldout(ShowToken, "Token");
            if (ShowToken)
            {
                EditorGUI.indentLevel++;
                Token = EditorGUILayout.TextField(Token);
                EditorGUI.indentLevel--;
            }

            string release = "\n    Name: " + Settings.GameName + " " + Settings.CurrentVersion;
            release += "\n    Tag: " + Prefix + Settings.CurrentVersion;

            EditorGUILayout.HelpBox("Release info:" + release, MessageType.Info);
        }
    }
}
