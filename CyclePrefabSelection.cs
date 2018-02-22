using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Vulpes
{
    [InitializeOnLoad]
    public sealed class CyclePrefabSelection 
    {
        static CyclePrefabSelection()
        {
            SceneView.onSceneGUIDelegate -= OnSceneGUI;
            SceneView.onSceneGUIDelegate += OnSceneGUI;
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            GameObject selection = Selection.activeGameObject;
            if (selection != null)
            {
                PrefabType prefabType = PrefabUtility.GetPrefabType(selection);
                if (prefabType == PrefabType.PrefabInstance && PrefabUtility.FindPrefabRoot(selection) == selection)
                {
                    Event e = Event.current;
                    if (e.type == EventType.scrollWheel && e.modifiers == EventModifiers.Alt)
                    {
                        Object prefab = PrefabUtility.GetPrefabParent(selection);
                        Object nextPrefab = null;
                        Object previousPrefab = null;
                        string assetDirectory = AssetDatabase.GetAssetPath(prefab).Replace(string.Format("{0}.prefab", prefab.name), "");
                        DirectoryInfo directoryInfo = new DirectoryInfo(assetDirectory);
                        FileInfo[] fileInfo = directoryInfo.GetFiles("*.prefab");
                        Object[] allPrefabs = new Object[fileInfo.Length];
                        int currentIndex = 0;

                        for (int i = 0; i < fileInfo.Length; i++)
                        {
                            string fullPath = fileInfo[i].FullName.Replace(@"\", "/");
                            string assetPath = "Assets" + fullPath.Replace(Application.dataPath, "");
                            Object prefabRef = AssetDatabase.LoadAssetAtPath(assetPath, typeof(Object));
                            allPrefabs[i] = prefabRef;
                            if (prefabRef == prefab)
                            {
                                currentIndex = i;
                            }
                        }

                        if (allPrefabs.Length > 1)
                        {
                            nextPrefab = allPrefabs[(currentIndex == allPrefabs.Length - 1 ? 0 : currentIndex + 1)];
                            previousPrefab = allPrefabs[(currentIndex == 0 ? allPrefabs.Length - 1 : currentIndex - 1)];
                        } else
                        {
                            nextPrefab = prefab;
                            previousPrefab = prefab;
                        }
                        
                        Object prefabToUse = null;
                        
                        if (e.delta.x > 0.0f || e.delta.y > 0.0f)
                        {
                            prefabToUse = nextPrefab;
                        } else if (e.delta.x < 0.0f || e.delta.y < 0.0f)
                        {
                            prefabToUse = previousPrefab;
                        }

                        GameObject replacement = PrefabUtility.InstantiatePrefab(prefabToUse) as GameObject;
                        replacement.transform.position = selection.transform.position;
                        replacement.transform.eulerAngles = selection.transform.eulerAngles;
                        replacement.transform.localScale = selection.transform.localScale;
                        replacement.transform.SetParent(selection.transform.parent);
                        Selection.activeGameObject = replacement;
                        Undo.RegisterCreatedObjectUndo(replacement, "Cycle Prefab");
                        Undo.RegisterFullObjectHierarchyUndo(selection, "Cycle Prefab");
                        Object.DestroyImmediate(selection);
                        EditorSceneManager.MarkAllScenesDirty();

                        e.Use();
                    }
                }
            }
        }
    }
}
