using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Shino.Tools.Editor
{
    public class VRCFuryToggleGenerator : EditorWindow
    {
        public GameObject targetRoot;
        public string menuPrefix = "";

        [MenuItem("Tools/Shino/VRCFury Toggle Generator")]
        public static void ShowWindow() => GetWindow<VRCFuryToggleGenerator>("VRCFury Toggle Gen");

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            targetRoot = (GameObject)EditorGUILayout.ObjectField("Target Root", targetRoot, typeof(GameObject), true);
            if (GUILayout.Button("Use Selected", GUILayout.Width(100)))
            {
                if (Selection.activeGameObject != null) targetRoot = Selection.activeGameObject;
            }
            EditorGUILayout.EndHorizontal();
            menuPrefix = EditorGUILayout.TextField("Menu Prefix", menuPrefix); 
            EditorGUILayout.HelpBox("Example: 'Clothing/Jackets'. Leave blank to put directly in the main menu.", MessageType.Info);

            GUILayout.Space(10);

            if (GUILayout.Button("1. Generate Toggles (Turn Off)") && targetRoot != null)
            {
                GenerateToggles();
            }

            if (GUILayout.Button("2. Assign Global Params to Detected Toggles") && targetRoot != null)
            {
                AssignGlobalParams();
            }
        }

        private void GenerateToggles()
        {
            Type vrcfuryType = GetTypeByName("VF.Model.VRCFury");
            Type toggleType = GetTypeByName("VF.Model.Feature.Toggle");
            Type actionType = GetTypeByName("VF.Model.StateAction.ObjectToggleAction");

            if (vrcfuryType == null || toggleType == null || actionType == null)
            {
                Debug.LogError("[VRCFury Gen] Could not find VRCFury classes.");
                return;
            }

            List<GameObject> validChildren = new List<GameObject>();
            ScanChildren(targetRoot.transform, validChildren);

            int addedCount = 0;

            foreach (GameObject child in validChildren)
            {
                Component vrcfComp = child.GetComponent(vrcfuryType);
                if (vrcfComp == null) vrcfComp = child.AddComponent(vrcfuryType);

                SerializedObject so = new SerializedObject(vrcfComp);
                SerializedProperty featuresProp = so.FindProperty("config.features");

                if (featuresProp == null) continue;

                featuresProp.arraySize++;
                SerializedProperty newFeature = featuresProp.GetArrayElementAtIndex(featuresProp.arraySize - 1);

                newFeature.managedReferenceValue = Activator.CreateInstance(toggleType);
                so.ApplyModifiedProperties(); 
                so.Update();

                string toggleMenuPath = child.name;
                if (!string.IsNullOrWhiteSpace(menuPrefix))
                {
                    toggleMenuPath = $"{menuPrefix.TrimEnd('/')}/{child.name}";
                }

                newFeature.FindPropertyRelative("name").stringValue = toggleMenuPath;
                newFeature.FindPropertyRelative("saved").boolValue = true;

                SerializedProperty stateProp = newFeature.FindPropertyRelative("state");
                SerializedProperty actionsProp = stateProp.FindPropertyRelative("actions");

                actionsProp.arraySize++;
                SerializedProperty newAction = actionsProp.GetArrayElementAtIndex(actionsProp.arraySize - 1);

                newAction.managedReferenceValue = Activator.CreateInstance(actionType);
                so.ApplyModifiedProperties();
                so.Update();

                newAction.FindPropertyRelative("obj").objectReferenceValue = child;
                newAction.FindPropertyRelative("mode").enumValueIndex = 1; 

                so.ApplyModifiedProperties();
                addedCount++;
            }

            Debug.Log($"[VRCFury Gen] Added {addedCount} individual toggles.");
        }

        private void AssignGlobalParams()
        {
            int togglesUpdated = 0;

            foreach (MonoBehaviour comp in targetRoot.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (comp == null || comp.GetType().Name != "VRCFury") continue;

                SerializedObject so = new SerializedObject(comp);
                bool modified = false;

                SerializedProperty features = so.FindProperty("config.features");
                if (features != null && features.isArray)
                {
                    for (int i = 0; i < features.arraySize; i++)
                    {
                        SerializedProperty feature = features.GetArrayElementAtIndex(i);
                        if (feature.managedReferenceFullTypename.Contains("Toggle"))
                            modified |= TrySetGlobalParam(feature);
                    }
                }

                SerializedProperty content = so.FindProperty("content");
                if (content != null && content.managedReferenceFullTypename.Contains("Toggle"))
                {
                    modified |= TrySetGlobalParam(content);
                }

                if (modified)
                {
                    so.ApplyModifiedProperties();
                    togglesUpdated++;
                }
            }

            Debug.Log($"[VRCFury Gen] Assigned global parameters to {togglesUpdated} toggles.");
        }

        private bool TrySetGlobalParam(SerializedProperty toggleFeature)
        {
            SerializedProperty nameProp = toggleFeature.FindPropertyRelative("name");
            SerializedProperty useGlobalParam = toggleFeature.FindPropertyRelative("useGlobalParam");
            SerializedProperty globalParam = toggleFeature.FindPropertyRelative("globalParam");

            if (nameProp != null && useGlobalParam != null && globalParam != null)
            {
                useGlobalParam.boolValue = true;

                globalParam.stringValue = nameProp.stringValue;
                return true;
            }
            return false;
        }

        private void ScanChildren(Transform current, List<GameObject> validObjects)
        {
            foreach (Transform child in current)
            {
                if (child.name == "Armature") continue; 
                validObjects.Add(child.gameObject);
                ScanChildren(child, validObjects);
            }
        }

        private static Type GetTypeByName(string className)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(className);
                if (type != null) return type;
            }
            return null;
        }
    }
}