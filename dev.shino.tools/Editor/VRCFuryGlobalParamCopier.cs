using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Shino.Tools.Editor
{
    public class VRCFuryGlobalParamCopier : EditorWindow
    {
        [SerializeField] private GameObject _targetObject;
        [SerializeField] private AnimatorController _fxController;

        private int _tab = 0;
        private readonly string[] _tabTitles = { "Add to FX Controller", "Create Preset" };

        [MenuItem("Tools/Shino/VRCFury Global Param Copier")]
        public static void ShowWindow() => GetWindow<VRCFuryGlobalParamCopier>("VRCFury Param Copier");

        private void OnGUI()
        {
            _tab = GUILayout.Toolbar(_tab, _tabTitles);
            GUILayout.Space(10);

            if (_tab == 0)
            {
                DrawFXControllerTab();
            }
            else
            {
                DrawPresetCreatorTab();
            }
        }

        private void DrawFXControllerTab()
        {
            _targetObject = (GameObject)EditorGUILayout.ObjectField("Target GameObject", _targetObject, typeof(GameObject), true);
            if (GUILayout.Button("Use Selected") && Selection.activeGameObject != null)
            {
                _targetObject = Selection.activeGameObject;
            }
            _fxController = (AnimatorController)EditorGUILayout.ObjectField("FX Controller", _fxController, typeof(AnimatorController), false);
            if (GUILayout.Button("Scan and Copy") && _targetObject && _fxController)
            {
                ScanAndCopy();
            }
        }

        private void DrawPresetCreatorTab()
        {
            _targetObject = (GameObject)EditorGUILayout.ObjectField("Target GameObject", _targetObject, typeof(GameObject), true);
            if (GUILayout.Button("Use Selected") && Selection.activeGameObject != null)
            {
                _targetObject = Selection.activeGameObject;
            }
            if (GUILayout.Button("Scan and Create Preset") && _targetObject)
            {
                ScanAndCreatePreset();
            }
        }

        private HashSet<string> ScanGlobalParameters(GameObject root)
        {
            HashSet<string> paramsFound = new HashSet<string>();
            if (root == null) return paramsFound;

            foreach (MonoBehaviour comp in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (comp == null || comp.GetType().Name != "VRCFury")
                {
                    continue;
                }

                SerializedObject so = new SerializedObject(comp);
                
                // Scan content
                SerializedProperty useGlobal = so.FindProperty("content.useGlobalParam");
                SerializedProperty globalParam = so.FindProperty("content.globalParam");
                if (useGlobal != null && useGlobal.boolValue && globalParam != null && !string.IsNullOrEmpty(globalParam.stringValue))
                {
                    paramsFound.Add(globalParam.stringValue);
                }

                // Scan features
                SerializedProperty features = so.FindProperty("config.features");
                if (features != null && features.isArray)
                {
                    for (int i = 0; i < features.arraySize; i++)
                    {
                        SerializedProperty feature = features.GetArrayElementAtIndex(i);
                        if (feature != null && feature.managedReferenceFullTypename.Contains("Toggle"))
                        {
                            SerializedProperty fUseGlobal = feature.FindPropertyRelative("useGlobalParam");
                            SerializedProperty fGlobalParam = feature.FindPropertyRelative("globalParam");
                            if (fUseGlobal != null && fUseGlobal.boolValue && fGlobalParam != null && !string.IsNullOrEmpty(fGlobalParam.stringValue))
                            {
                                paramsFound.Add(fGlobalParam.stringValue);
                            }
                        }
                    }
                }
            }
            return paramsFound;
        }

        private void ScanAndCopy()
        {
            if (_targetObject == null || _fxController == null)
            {
                return;
            }

            HashSet<string> paramsToAdd = ScanGlobalParameters(_targetObject);

            int addedCount = 0;
            foreach (string pName in paramsToAdd)
            {
                if (!System.Array.Exists(_fxController.parameters, p => p.name == pName))
                {
                    _fxController.AddParameter(pName, AnimatorControllerParameterType.Bool);
                    addedCount++;
                }
            }
            Debug.Log($"[VRCFury Copier] Success! Added {addedCount} new boolean parameters.");
            EditorUtility.DisplayDialog("Success", $"Added {addedCount} new boolean parameters to FX Controller.", "OK");
        }

        private void ScanAndCreatePreset()
        {
            if (_targetObject == null)
            {
                Debug.LogWarning("[VRCFury Preset Creator] No target GameObject selected.");
                return;
            }

            HashSet<string> paramsToAdd = ScanGlobalParameters(_targetObject);

            if (paramsToAdd.Count == 0)
            {
                Debug.LogWarning("[VRCFury Preset Creator] No global parameters found on VRCFury toggles under the target GameObject.");
                EditorUtility.DisplayDialog("Warning", "No VRCFury global parameters found on target GameObject.", "OK");
                return;
            }

            System.Type driverType = GetTypeByName("VRC.SDK3.Avatars.Components.VRCAvatarParameterDriver");
            if (driverType == null)
            {
                Debug.LogError("[VRCFury Preset Creator] Could not find VRCAvatarParameterDriver type. Is the VRCSDK installed?");
                EditorUtility.DisplayDialog("Error", "Could not find VRCAvatarParameterDriver type. Is the VRCSDK installed?", "OK");
                return;
            }

            string defaultName = $"{_targetObject.name}Preset";
            string path = EditorUtility.SaveFilePanelInProject(
                "Save Preset",
                defaultName,
                "preset",
                "Please enter a file name to save the parameter driver preset to"
            );

            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            UnityEngine.Object tempObj = null;
            if (typeof(ScriptableObject).IsAssignableFrom(driverType))
            {
                tempObj = ScriptableObject.CreateInstance(driverType);
            }
            else if (typeof(MonoBehaviour).IsAssignableFrom(driverType))
            {
                GameObject go = new GameObject("Temp");
                tempObj = go.AddComponent(driverType);
            }

            if (tempObj == null)
            {
                Debug.LogError("[VRCFury Preset Creator] Failed to create a temporary instance of VRCAvatarParameterDriver.");
                return;
            }

            try
            {
                SerializedObject so = new SerializedObject(tempObj);
                SerializedProperty parametersProp = so.FindProperty("parameters");
                if (parametersProp == null)
                {
                    Debug.LogError("[VRCFury Preset Creator] Serialized property 'parameters' not found on VRCAvatarParameterDriver.");
                    return;
                }

                parametersProp.ClearArray();
                int idx = 0;
                foreach (string pName in paramsToAdd)
                {
                    parametersProp.InsertArrayElementAtIndex(idx);
                    SerializedProperty element = parametersProp.GetArrayElementAtIndex(idx);

                    SerializedProperty nameProp = element.FindPropertyRelative("name");
                    SerializedProperty sourceProp = element.FindPropertyRelative("source");
                    SerializedProperty typeProp = element.FindPropertyRelative("type");
                    SerializedProperty valueProp = element.FindPropertyRelative("value");
                    SerializedProperty valueMinProp = element.FindPropertyRelative("valueMin");
                    SerializedProperty valueMaxProp = element.FindPropertyRelative("valueMax");
                    SerializedProperty chanceProp = element.FindPropertyRelative("chance");
                    SerializedProperty preventRepeatsProp = element.FindPropertyRelative("preventRepeats");
                    SerializedProperty convertRangeProp = element.FindPropertyRelative("convertRange");
                    SerializedProperty sourceMinProp = element.FindPropertyRelative("sourceMin");
                    SerializedProperty sourceMaxProp = element.FindPropertyRelative("sourceMax");
                    SerializedProperty destMinProp = element.FindPropertyRelative("destMin");
                    SerializedProperty destMaxProp = element.FindPropertyRelative("destMax");

                    if (nameProp != null) nameProp.stringValue = pName;
                    if (sourceProp != null) sourceProp.stringValue = "";
                    if (typeProp != null) typeProp.intValue = 0; // Set
                    if (valueProp != null) valueProp.floatValue = 0f;
                    if (valueMinProp != null) valueMinProp.floatValue = 0f;
                    if (valueMaxProp != null) valueMaxProp.floatValue = 0f;
                    if (chanceProp != null) chanceProp.floatValue = 0f;
                    if (preventRepeatsProp != null) preventRepeatsProp.boolValue = false;
                    if (convertRangeProp != null) convertRangeProp.boolValue = false;
                    if (sourceMinProp != null) sourceMinProp.floatValue = 0f;
                    if (sourceMaxProp != null) sourceMaxProp.floatValue = 0f;
                    if (destMinProp != null) destMinProp.floatValue = 0f;
                    if (destMaxProp != null) destMaxProp.floatValue = 0f;

                    idx++;
                }

                SerializedProperty localOnlyProp = so.FindProperty("localOnly");
                if (localOnlyProp != null) localOnlyProp.boolValue = false;

                SerializedProperty debugStringProp = so.FindProperty("debugString");
                if (debugStringProp != null) debugStringProp.stringValue = "";

                so.ApplyModifiedProperties();

                // Create and write Preset
                UnityEditor.Presets.Preset preset = new UnityEditor.Presets.Preset(tempObj);
                AssetDatabase.CreateAsset(preset, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log($"[VRCFury Preset Creator] Success! Created preset at {path} with {paramsToAdd.Count} parameters.");
                EditorUtility.DisplayDialog("Success", $"Created preset with {paramsToAdd.Count} parameters at:\n{path}", "OK");
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                EditorUtility.DisplayDialog("Error", $"An error occurred: {e.Message}", "OK");
            }
            finally
            {
                if (tempObj != null)
                {
                    if (tempObj is GameObject go)
                    {
                        DestroyImmediate(go);
                    }
                    else
                    {
                        DestroyImmediate(tempObj);
                    }
                }
            }
        }

        private static System.Type GetTypeByName(string className)
        {
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(className);
                if (type != null)
                {
                    return type;
                }
            }
            return null;
        }
    }
}