using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Vulpes
{
    [InitializeOnLoad]
    public sealed class CyclePrefabSelection 
    {
        private static EventModifiers cycleModifierKey = EventModifiers.Alt; // Note: Only Alt, Control, Shift, and Caps Lock seem to work.

        static CyclePrefabSelection()
        {
            SceneView.onSceneGUIDelegate -= OnSceneGUI;
            SceneView.onSceneGUIDelegate += OnSceneGUI;
            cycleModifierKey = (EventModifiers)EditorPrefs.GetInt("CyclePrefabSelectionCycleModifierKey", 4);
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            Object[] selection = Selection.objects;
            Object[] newSelection = new Object[selection.Length];

            if (selection.Length > 0)
            {
                Event e = Event.current;

                if (e.type == EventType.ScrollWheel && e.modifiers == cycleModifierKey)
                {
                    for (int selectionIndex = 0; selectionIndex < selection.Length; selectionIndex++)
                    {
                        GameObject selectionGameObject = (GameObject)selection[selectionIndex];
                        PrefabType prefabType = PrefabUtility.GetPrefabType(selectionGameObject);

                        if (prefabType != PrefabType.PrefabInstance)
                        {
                            newSelection[selectionIndex] = selectionGameObject;
                            continue;
                        }

                        GameObject prefabRoot = PrefabUtility.FindPrefabRoot(selectionGameObject);

                        if (prefabRoot != selectionGameObject)
                        {
                            selection[selectionIndex] = prefabRoot;
                            Selection.objects = selection;
                            selectionGameObject = prefabRoot;
                        }

                        Object prefab = PrefabUtility.GetPrefabParent(selectionGameObject);
                        Object nextPrefab = null;
                        Object previousPrefab = null;
                        string assetDirectory = AssetDatabase.GetAssetPath(prefab).Replace(string.Format("{0}.prefab", prefab.name), "");
                        DirectoryInfo directoryInfo = new DirectoryInfo(assetDirectory);
                        FileInfo[] fileInfo = directoryInfo.GetFiles("*.prefab");
                        // TODO Find alternate solution that doesn't involve caching all the prefabs, we only care about two of them after all.
                        Object[] allPrefabs = new Object[fileInfo.Length];
                        int currentIndex = 0;

                        for (int fileInfoIndex = 0; fileInfoIndex < fileInfo.Length; fileInfoIndex++)
                        {
                            string fullPath = fileInfo[fileInfoIndex].FullName.Replace(@"\", "/");
                            string assetPath = "Assets" + fullPath.Replace(Application.dataPath, "");
                            Object prefabRef = AssetDatabase.LoadAssetAtPath(assetPath, typeof(Object));
                            allPrefabs[fileInfoIndex] = prefabRef;

                            if (prefabRef == prefab)
                            {
                                currentIndex = fileInfoIndex;
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

                        // TODO Can probably just use the event delta earlier to determine whether we want the next or previous prefab and avoid caching both prior.
                        if (e.delta.x > 0.0f || e.delta.y > 0.0f)
                        {
                            prefabToUse = nextPrefab;
                        } else if (e.delta.x < 0.0f || e.delta.y < 0.0f)
                        {
                            prefabToUse = previousPrefab;
                        }

                        GameObject replacement = PrefabUtility.InstantiatePrefab(prefabToUse) as GameObject;
                        replacement.transform.position = selectionGameObject.transform.position;
                        replacement.transform.eulerAngles = selectionGameObject.transform.eulerAngles;
                        replacement.transform.localScale = selectionGameObject.transform.localScale;
                        replacement.transform.SetParent(selectionGameObject.transform.parent);
                        newSelection[selectionIndex] = replacement;
                        Undo.RegisterCreatedObjectUndo(replacement, "Cycle Prefab");
                        Undo.RegisterFullObjectHierarchyUndo(selectionGameObject, "Cycle Prefab");
                        Object.DestroyImmediate(selectionGameObject);
                        EditorSceneManager.MarkAllScenesDirty();
                    }

                    Selection.objects = newSelection;
                    e.Use();
                }
            }
        }

        [PreferenceItem("Prefabs")]
        public static void PreferencesGUI()
        {
            cycleModifierKey = (EventModifiers)EditorGUILayout.EnumPopup("Cycle Modifier Key", cycleModifierKey);
            if (GUI.changed)
            {
                EditorPrefs.SetInt("CyclePrefabSelectionCycleModifierKey", (int)cycleModifierKey);
            }
        }
    }
}
