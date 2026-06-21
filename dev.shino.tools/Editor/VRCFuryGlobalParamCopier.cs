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

        [MenuItem("Tools/Shino/VRCFury Global Param Copier")]
        public static void ShowWindow() => GetWindow<VRCFuryGlobalParamCopier>("VRCFury Param Copier");

        private void OnGUI()
        {
            _targetObject = (GameObject)EditorGUILayout.ObjectField("Target GameObject", _targetObject, typeof(GameObject), true);
            if (GUILayout.Button("Use Selected") && Selection.activeGameObject != null) _targetObject = Selection.activeGameObject;
            _fxController = (AnimatorController)EditorGUILayout.ObjectField("FX Controller", _fxController, typeof(AnimatorController), false);
            if (GUILayout.Button("Scan and Copy") && _targetObject && _fxController) ScanAndCopy();
        }

        private void ScanAndCopy()
        {
            HashSet<string> paramsToAdd = new HashSet<string>();
            foreach (MonoBehaviour comp in _targetObject.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (comp == null || comp.GetType().Name != "VRCFury") continue;

                SerializedObject so = new SerializedObject(comp);
                // Pull directly from the 'content' object shown in your screenshot
                SerializedProperty useGlobal = so.FindProperty("content.useGlobalParam");
                SerializedProperty globalParam = so.FindProperty("content.globalParam");

                if (useGlobal != null && useGlobal.boolValue && globalParam != null && !string.IsNullOrEmpty(globalParam.stringValue))
                {
                    paramsToAdd.Add(globalParam.stringValue);
                }
            }

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
        }
    }
}