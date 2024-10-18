
using System;
using System.IO;
using UnityEngine;

namespace Packages.SMCPConfigurator.Editor
{
    sealed class CheckVersion
    {
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