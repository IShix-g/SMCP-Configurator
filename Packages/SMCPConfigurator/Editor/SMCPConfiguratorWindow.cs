
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEditor;

namespace Packages.SMCPConfigurator.Editor
{
    public class SMCPConfiguratorWindow : EditorWindow
    {
        const string _packageUrl = "https://raw.githubusercontent.com/IShix-g/SMCP-Configurator/main/Packages/SMCPConfigurator/package.json";
        const string _packagePath = "Packages/com.ishix.smcpconfigurator/";
        const string _gitUrl = "https://github.com/IShix-g/SMCP-Configurator";
        static readonly Dictionary<string, string[]> s_assemblyDefinitions = new Dictionary<string, string[]>
        {
            { "LogicAndModel", Array.Empty<string>() },
            { "View", new[] { "LogicAndModel" } },
            { "Others", new[] { "LogicAndModel", "View" } }
        };
        
        string _rootPath = "Assets/_Projects/Scripts/";
        string _currentVersion;
        bool _isStartCheckVersion;
        CancellationTokenSource _tokenSource;
        
        [MenuItem("Window/SMCP Configurator")]
        static void ShowWindow()
        {
            var window = GetWindow<SMCPConfiguratorWindow>();
            window.minSize = new Vector2(400, 400);
            window.titleContent = new GUIContent("SMCP Configurator");
            window.Show();
        }

        void OnEnable() => _currentVersion = "v" + CheckVersion.GetCurrent(_packagePath);

        void OnDestroy()
        {
            _tokenSource?.SafeCancelAndDispose();
            _isStartCheckVersion = false;
        }
        
        void OnGUI()
        {
            {
                var style = new GUIStyle()
                {
                    padding = new RectOffset(10, 10, 10, 10)
                };
                GUILayout.BeginVertical(style);
            }
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("GitHub"))
            {
                Application.OpenURL(_gitUrl);
            }
            EditorGUI.BeginDisabledGroup(_isStartCheckVersion);
            if (GUILayout.Button("Check for Update"))
            {
                StartCheckUpdate();
            }
            EditorGUI.EndDisabledGroup();
            GUILayout.EndHorizontal();
            
            {
                var style = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 13,
                    padding = new RectOffset(0, 0, 10, 5),
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true
                };
                GUILayout.Label("SMCP Configurator", style);
            }
            {
                var style = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    padding = new RectOffset(0, 0, 0, 10)
                };
                EditorGUILayout.LabelField(_currentVersion, style);
            }
            
            EditorGUILayout.HelpBox("Create the necessary directories and assembly definition for SMCP.", MessageType.Info);
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Root Directory", GUILayout.MaxWidth(90));
            _rootPath = GUILayout.TextField(_rootPath);
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
            if (GUILayout.Button("Create Directories", GUILayout.Height(35))
                && ValidDirectory(_rootPath))
            {
                GenerateAssemblyDefinitions(_rootPath);
            }

            GUILayout.Space(5);
            EditorGUILayout.HelpBox("In SMCP, I recommend using DI (Dependency Injection), especially VContainer for its speed and minimal code requirements.", MessageType.Info);
            GUILayout.Space(5);

#if ENABLE_VCONTAINER
            EditorGUI.BeginDisabledGroup(true);
            GUILayout.Button("VContainer is installed.", GUILayout.Height(35));
            EditorGUI.EndDisabledGroup();
#else
            if (GUILayout.Button("Installing VContainer", GUILayout.Height(35)))
            {
                Application.OpenURL("https://github.com/hadashiA/VContainer?tab=readme-ov-file#installation");
            }
#endif

            GUILayout.Space(5);
            EditorGUILayout.HelpBox("Enabling Source Generator improves performance, so it is recommended.", MessageType.Info);
            GUILayout.Space(5);
#if !ENABLE_VCONTAINER
            EditorGUI.BeginDisabledGroup(true);
            GUILayout.Button("VContainer is not installed.", GUILayout.Height(35));
            EditorGUI.EndDisabledGroup();
#else
            if (GUILayout.Button("Enable VContainer's Source Generator", GUILayout.Height(35)))
            {
                Application.OpenURL("https://vcontainer.hadashikick.jp/optimization/source-generator");
            }
#endif

            GUILayout.EndVertical();
        }

        void StartCheckUpdate()
        {
            _isStartCheckVersion = true;
            _tokenSource = new CancellationTokenSource();
            
            CheckVersion.GetVersionOnServerAsync(_packageUrl)
                .SafeContinueWith(task =>
                {
                    var version = task.Result;
                    
                    if (!string.IsNullOrEmpty(version))
                    {
                        var comparisonResult = default(int);
                        var current = new Version(_currentVersion.TrimStart('v').Trim());
                        var server = new Version(version.Trim());
                        comparisonResult = current.CompareTo(server);
                        version = "v" + version;
                            
                        if (comparisonResult >= 0)
                        {
                            EditorUtility.DisplayDialog("Check for Update",
                                "Local: " + _currentVersion + "  GitHub: " + version + "\nThe current version is the latest release.",
                                "Close");
                        }
                        else
                        {
                            EditorUtility.DisplayDialog(_currentVersion + " -> " + version,
                                "There is a newer version (" + version + "), please update from Package Manager.",
                                "Close");
                        }
                    }
                    _isStartCheckVersion = false;
                    _tokenSource.Dispose();
                    _tokenSource = default;
                }, _tokenSource.Token);
        }
        
        static void GenerateAssemblyDefinitions(string rootDirectory)
        {
            var generatedPaths = s_assemblyDefinitions
                .Select(kvp => CreateAssemblyDefinition(rootDirectory, kvp.Key, kvp.Value))
                .ToList();
            AssetDatabase.Refresh();
            Debug.Log($"Assembly Definition files generated.\n- {string.Join("\n- ", generatedPaths)}\n");
        }
        
        static string CreateAssemblyDefinition(string rootDirectory, string assemblyName, string[] references)
        {
            var asmdef = new AssemblyDefinition
            {
                name = assemblyName,
                references = references
            };
            var path = Path.Combine(rootDirectory, assemblyName, assemblyName + ".asmdef");
            CreateDirectory(path);
            var json = JsonUtility.ToJson(asmdef, true);
            File.WriteAllText(path, json);
            return path;
        }
        
        static void CreateDirectory(string path)
        {
            if (!string.IsNullOrEmpty(Path.GetExtension(path)))
            {
                path = Path.GetDirectoryName(path);
            }
            if (string.IsNullOrEmpty(path))
            {
                return;
            }
            var dirs = path.Split('/');
            var combinePath = dirs[0];
            foreach (var dir in dirs.Skip(1))
            {
                if (!AssetDatabase.IsValidFolder(combinePath + '/' + dir))
                {
                    AssetDatabase.CreateFolder(combinePath, dir);
                }
                combinePath += '/' + dir;
            }
        }

        static bool ValidDirectory(string dir, bool showError = true)
        {
            if (string.IsNullOrEmpty(dir))
            {
                if (showError)
                {
                    Debug.LogError("Directory path is empty. Please set it.");
                }
                return false;
            }
            if (!dir.StartsWith("Assets/"))
            {
                if (showError)
                {
                    Debug.LogError("Please specify the contents under the Assets/ directory. " + dir);
                }
                return false;
            }
            if (Path.HasExtension(dir))
            {
                if (showError)
                {
                    Debug.LogError("Please specify a directory for the path. " + dir);
                }
                return false;
            }
            return true;
        }
        
        [Serializable]
        class AssemblyDefinition
        {
            public string name;
            public string[] references;
        }
    }
}