
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;

namespace SMCPConfigurator.Editor
{
    sealed class CheckVersion
    {
        internal static async Task<string> GetVersionOnServerAsync(string gitUrl)
        {
            try
            {
                using var client = new HttpClient();
                var response = await client.GetAsync(gitUrl);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return GetVersionByJson(json);
                }
                Debug.LogError("GetVersion error: " + response.ReasonPhrase);
                return string.Empty;
            }
            catch (HttpRequestException ex)
            {
                Debug.LogError("GetVersion exception: " + ex.Message);
                return string.Empty;
            }
        }
        
        internal static string GetCurrent(string packagePath)
        {
            var path = Path.Combine(packagePath, "package.json");
            var json = File.ReadAllText(path);
            return GetVersionByJson(json);
        }
        
        static string GetVersionByJson(string json)
        {
            var obj = JsonUtility.FromJson<Package>(json);
            return obj != default ? obj.version : "--";
        }
        
        [Serializable]
        sealed class Package
        {
            public string version;
        }
    }
}