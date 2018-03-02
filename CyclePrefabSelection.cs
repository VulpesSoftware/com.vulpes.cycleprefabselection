using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Vulpes
{
    [InitializeOnLoad]
    public sealed class CyclePrefabSelection 
    {
        private static ValidEventModifiers cycleModifierKey = ValidEventModifiers.Alt;
        private static bool invertScrollDirection = false;

        public enum ValidEventModifiers
        {
            Shift = 1,
            Control = 2,
            Alt = 4,
        }

        static CyclePrefabSelection()
        {
            SceneView.onSceneGUIDelegate -= OnSceneGUI;
            SceneView.onSceneGUIDelegate += OnSceneGUI;
            cycleModifierKey = (ValidEventModifiers)EditorPrefs.GetInt("CyclePrefabSelectionCycleModifierKey", 4);
            invertScrollDirection = EditorPrefs.GetBool("CyclePrefabSelectionInvertScrollDirection", false);
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            Object[] selection = Selection.objects;
            Object[] newSelection = new Object[selection.Length];

            if (selection.Length > 0)
            {
                Event e = Event.current;

                if (e.type == EventType.ScrollWheel && e.modifiers == (EventModifiers)cycleModifierKey)
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
                        Object prefabToUse = null;
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

                        float scrollDelta = e.delta.x + e.delta.y;
                        if (invertScrollDirection)
                        {
                            scrollDelta = -scrollDelta;
                        }

                        if (allPrefabs.Length > 1)
                        {
                            if (scrollDelta > 0.0f)
                            {
                                prefabToUse = allPrefabs[(currentIndex == allPrefabs.Length - 1 ? 0 : currentIndex + 1)];
                            } else if (scrollDelta < 0.0f)
                            {
                                prefabToUse = allPrefabs[(currentIndex == 0 ? allPrefabs.Length - 1 : currentIndex - 1)];
                            }
                        } else
                        {
                            prefabToUse = prefab;
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
            cycleModifierKey = (ValidEventModifiers)EditorGUILayout.EnumPopup("Cycle Modifier Key", cycleModifierKey);
            invertScrollDirection = EditorGUILayout.Toggle("Invert Scroll Direction", invertScrollDirection);
            if (GUI.changed)
            {
                EditorPrefs.SetInt("CyclePrefabSelectionCycleModifierKey", (int)cycleModifierKey);
                EditorPrefs.SetBool("CyclePrefabSelectionInvertScrollDirection", invertScrollDirection);
            }
        }
    }
}
