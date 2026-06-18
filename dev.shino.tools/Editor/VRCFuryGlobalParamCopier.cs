using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class VRCFuryGlobalParamCopier : EditorWindow
{
    public GameObject targetObject;
    public AnimatorController fxController;

    [MenuItem("Tools/Shino/VRCFury Global Param Copier")]
    public static void ShowWindow() => GetWindow<VRCFuryGlobalParamCopier>("VRCFury Param Copier");

    private void OnGUI()
    {
        targetObject = (GameObject)EditorGUILayout.ObjectField("Target GameObject", targetObject, typeof(GameObject), true);
        if (GUILayout.Button("Use Selected") && Selection.activeGameObject != null) targetObject = Selection.activeGameObject;
        fxController = (AnimatorController)EditorGUILayout.ObjectField("FX Controller", fxController, typeof(AnimatorController), false);
        if (GUILayout.Button("Scan and Copy") && targetObject && fxController) ScanAndCopy();
    }

    private void ScanAndCopy()
    {
        HashSet<string> paramsToAdd = new HashSet<string>();
        foreach (MonoBehaviour comp in targetObject.GetComponentsInChildren<MonoBehaviour>(true))
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
            if (!System.Array.Exists(fxController.parameters, p => p.name == pName))
            {
                fxController.AddParameter(pName, AnimatorControllerParameterType.Bool);
                addedCount++;
            }
        }
        Debug.Log($"[VRCFury Copier] Success! Added {addedCount} new boolean parameters.");
    }
}