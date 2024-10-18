
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace Packages.SMCPConfigurator.Editor
{
    [Serializable]
    class AssemblyDefinition
    {
        public string name;
        public string[] references;
    }
    
    public class SMCPConfiguratorWindow : EditorWindow
    {
        string _rootPath = "Assets/_Projects/Scripts/";
        
        [MenuItem("Window/SMCP Configurator")]
        static void ShowWindow()
        {
            var window = GetWindow<SMCPConfiguratorWindow>();
            window.minSize = new Vector2(400, 400);
            window.titleContent = new GUIContent("SMCP Configurator");
            window.Show();
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
            {
                var style = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 13,
                    padding = new RectOffset(0, 0, 0, 10),
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true
                };
                GUILayout.Label("SMCP Configurator", style);
            }
            
            EditorGUILayout.HelpBox("Create the necessary directories and assembly definition for SMCP.", MessageType.Info);
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Directory", GUILayout.MaxWidth(70));
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
            if (GUILayout.Button("Enable VContainer's Source Generator", GUILayout.Height(35)))
            {
                Application.OpenURL("https://vcontainer.hadashikick.jp/optimization/source-generator");
            }
            
            GUILayout.EndVertical();
        }
        
        static void GenerateAssemblyDefinitions(string rootDirectory)
        {
            var path1 = CreateAssemblyDefinition(rootDirectory, "LogicAndModel");
            var path2 = CreateAssemblyDefinition(rootDirectory, "View", new[] { "LogicAndModel" });
            var path3 = CreateAssemblyDefinition(rootDirectory, "Others", new[] { "LogicAndModel", "View" });
            AssetDatabase.Refresh();
            Debug.Log("Assembly Definition files generated.\n-" + path1 + "\n-" + path2 + "\n-" + path3 + "\n");
        }
        
        static string CreateAssemblyDefinition(string rootDirectory, string assemblyName, string[] references = null)
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
    }
}