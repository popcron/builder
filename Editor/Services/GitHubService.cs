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

        private const string OwnerKey = "Popcron.Builder.GitHubService.Owner";
        private const string RepositoryKey = "Popcron.Builder.GitHubService.Repository";
        private const string TokenKey = "Popcron.Builder.GitHubService.Token";
        private const string ShowTokenKey = "Popcron.Builder.GitHubService.ShowToken";
        private const string UserAgent = "Popcron.Builder.GitHubService";

        private static string owner = null;
        private static string repository = null;
        private static bool? showToken = null;

        public static string Owner
        {
            get
            {
                if (owner == null)
                {
                    owner = EditorPrefs.GetString(PlayerSettings.productGUID + OwnerKey, PlayerSettings.companyName);
                }

                return owner;
            }
            set
            {
                if (owner != value)
                {
                    owner = value;
                    EditorPrefs.SetString(PlayerSettings.productGUID + OwnerKey, value);
                }
            }
        }

        public static string Repository
        {
            get
            {
                if (repository == null)
                {
                    repository = EditorPrefs.GetString(PlayerSettings.productGUID + RepositoryKey, PlayerSettings.productName);
                }

                return repository;
            }
            set
            {
                if (repository != value)
                {
                    repository = value;
                    EditorPrefs.SetString(PlayerSettings.productGUID + RepositoryKey, value);
                }
            }
        }

        public static string Token
        {
            get
            {
                return EditorPrefs.GetString(PlayerSettings.productGUID + TokenKey);
            }
            set
            {
                EditorPrefs.SetString(PlayerSettings.productGUID + TokenKey, value);
            }
        }

        public static bool ShowToken
        {
            get
            {
                if (showToken == null)
                {
                    showToken = EditorPrefs.GetBool(PlayerSettings.productGUID + ShowTokenKey);
                }

                return showToken.Value;
            }
            set
            {
                if (showToken != value)
                {
                    showToken = value;
                    EditorPrefs.SetBool(PlayerSettings.productGUID + ShowTokenKey, value);
                }
            }
        }

        public override string Name => "GitHub";

        public override string URL => "https://github.com/" + Owner + "/" + Repository;

        public override bool CanUploadTo
        {
            get
            {
                return EditorPrefs.GetBool(Name + "_" + Owner + "_" + Repository, false);
            }
            set
            {
                EditorPrefs.SetBool(Name + "_" + Owner + "_" + Repository, value);
            }
        }

        public override async Task Upload(string path, string version, string platform)
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new Exception("GitHub service doesn't have an access token.");
            }

            string releaseName = "version " + version;
            string auth = "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(Owner + ":" + Token));
            bool releaseAlreadyExists = false;
            int releasesAmount = 0;

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
                        data = "{\"releases\":" + data + "}";
                        var releases = JsonUtility.FromJson<ReleasesResponse>(data);
                        releasesAmount = releases.releases.Count + 1;
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
                var createRequest = new CreateRequest
                {
                    name = releaseName,
                    tag_name = "v" + releasesAmount
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
                                Builder.Print("GitHub: Release with tag " + createRequest.tag_name + " already exists", MessageType.Error);
                                return;
                            }

                            string scrapedId = data.Substring(data.IndexOf("id") + 4);
                            id = int.Parse(scrapedId.Substring(0, scrapedId.IndexOf(",")));
                        }
                    }
                }

                string archiveName = Path.GetFileName(path);
                byte[] archiveData = File.ReadAllBytes(path);
                string url = "https://uploads.github.com/repos/" + Owner + "/" + Repository + "/releases/" + id + "/assets?name=" + archiveName;
                using (var httpClient = new HttpClient())
                {
                    using (var request = new HttpRequestMessage(new HttpMethod("POST"), url))
                    {
                        request.Headers.TryAddWithoutValidation("User-Agent", UserAgent);
                        request.Headers.TryAddWithoutValidation("Authorization", auth);

                        string stringData = Encoding.UTF8.GetString(archiveData);
                        request.Content = new StringContent(stringData, Encoding.UTF8, "application/zip");

                        var response = await httpClient.SendAsync(request);
                    }
                }

                Builder.Print("GitHub: Finished", MessageType.Info);
            }
            else
            {
                Builder.Print("GitHub: Release with version " + version + " already exists", MessageType.Error);
            }
        }

        public override void OnGUI()
        {
            Owner = EditorGUILayout.TextField("Owner", Owner);
            Repository = EditorGUILayout.TextField("Repository", Repository);

            ShowToken = EditorGUILayout.Foldout(ShowToken, "Token");
            if (ShowToken)
            {
                EditorGUI.indentLevel++;
                Token = EditorGUILayout.TextField(Token);
                EditorGUI.indentLevel--;
            }
        }
    }
}
