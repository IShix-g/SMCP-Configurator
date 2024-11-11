
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEditor;

namespace SMCPConfigurator.Editor
{
    public class SMCPConfiguratorWindow : EditorWindow
    {
        const string _gitUrl = "https://github.com/IShix-g/SMCP-Configurator";
        const string _packageJsonUrl = "https://raw.githubusercontent.com/IShix-g/SMCP-Configurator/main/Packages/SMCPConfigurator/package.json";
        const string _packageName = "com.ishix.smcpconfigurator";
        const string _packagePath = "Packages/com.ishix.smcpconfigurator/";
        const string _gitVContainerPackageName = "jp.hadashikick.vcontainer";
        const string _gitVContainerUrl = "https://github.com/hadashiA/VContainer.git?path=VContainer/Assets/VContainer";
        const string _gitInstallUrl = _gitUrl + ".git?path=Packages/SMCPConfigurator";
        static readonly Dictionary<string, string[]> s_assemblyDefinitions = new Dictionary<string, string[]>
        {
            { "LogicAndModel", Array.Empty<string>() },
            { "View", new[] { "LogicAndModel" } },
            { "Others", new[] { "LogicAndModel", "View" } }
        };
        
        [SerializeField] string _rootPath = "Assets/_Projects/Scripts/";
        bool _isProcessing;
        string _currentVersion;
        GUIContent _folderIcon;
        readonly PackageInstaller _packageInstaller = new ();
        CancellationTokenSource _tokenSource;
        
        [MenuItem("Window/SMCP Configurator")]
        static void ShowWindow()
        {
            var window = GetWindow<SMCPConfiguratorWindow>();
            window.minSize = new Vector2(400, 400);
            window.titleContent = new GUIContent("SMCP Configurator");
            window.Show();
        }
        
        void OnEnable()
        {
            _currentVersion = "v" + CheckVersion.GetCurrent(_packagePath);
            _folderIcon = EditorGUIUtility.IconContent("Folder Icon");
            Undo.undoRedoPerformed += Repaint;
        }
        
        void OnDisable() => Undo.undoRedoPerformed -= Repaint;
        
        void OnDestroy()
        {
            if (_packageInstaller.IsProcessing)
            {
                _packageInstaller.Cancel();
            }
            _tokenSource?.SafeCancelAndDispose();
            EditorUtility.ClearProgressBar();
            _isProcessing = false;
        }
        
        void OnGUI()
        {
            EditorGUI.BeginDisabledGroup(_isProcessing);
            
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
            if (GUILayout.Button("Check for Update"))
            {
                StartCheckUpdate();
            }
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
            GUILayout.Label("Script Directory", GUILayout.MaxWidth(90));
            _rootPath = GUILayout.TextField(_rootPath);
            var buttonClicked = GUILayout.Button(_folderIcon, GUILayout.Width(35), GUILayout.Height(EditorGUIUtility.singleLineHeight));
            GUILayout.EndHorizontal();

            if (buttonClicked)
            {
                var selectedPath = EditorUtility.OpenFolderPanel(
                    "Select root path",
                    string.IsNullOrEmpty(_rootPath)
                        ? "Assets/"
                        : _rootPath, "Select root path");
                
                if (!string.IsNullOrEmpty(selectedPath)
                    && _rootPath != selectedPath)
                {
                    Undo.RecordObject(this, "Root Path Update");
                    
                    _rootPath = selectedPath;
                    var assetsIndex = _rootPath.IndexOf("Assets", StringComparison.Ordinal);
                    if (assetsIndex >= 0)
                    {
                        _rootPath = _rootPath.Substring(assetsIndex);
                        if (!string.IsNullOrEmpty(_rootPath)
                            && !_rootPath.EndsWith('/'))
                        {
                            _rootPath += "/";
                        }
                    }
                    else
                    {
                        _rootPath = string.Empty;
                        Debug.LogError("Please select the directory path under Assets.");
                    }
                    
                    EditorUtility.SetDirty(this);
                }
            }
            
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
            var vContainerButtonTitle = "Update VContainer";
#else
            var vContainerButtonTitle = "Installing VContainer";
#endif
            if (GUILayout.Button(vContainerButtonTitle, GUILayout.Height(35)))
            {
                _packageInstaller.InstallAsync(_gitVContainerUrl, _gitVContainerPackageName).Handled();
            }
            
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
            
            EditorGUI.EndDisabledGroup();
        }

        void StartCheckUpdate()
        {
            _tokenSource = new CancellationTokenSource();
            
            CheckVersion.GetVersionOnServerAsync(_packageJsonUrl)
                .ContinueOnMainThread(task =>
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
                            EditorUtility.DisplayDialog("You have the latest version.",
                                "Editor: " + _currentVersion + "  GitHub: " + version + "\nThe current version is the latest release.",
                                "Close");
                        }
                        else
                        {
                            var isOpen = EditorUtility.DisplayDialog(_currentVersion + " -> " + version,
                                "There is a newer version (" + version + "), please update from Package Manager.",
                                "Update",
                                "Close");

                            if (isOpen)
                            {
                                _packageInstaller.InstallAsync(_gitInstallUrl, _packageName).Handled();
                            }
                        }
                    }
                    _tokenSource.Dispose();
                    _tokenSource = default;
                },
                cancellationToken: _tokenSource.Token);
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