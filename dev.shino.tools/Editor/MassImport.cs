using UnityEngine;
using UnityEditor;
using System.IO;

public class MassPackageImporter : EditorWindow
{
    [MenuItem("Tools/Shino/Mass Importer")]
    public static void ImportPackages()
    {
        // Open folder selection dialog
        string folderPath = EditorUtility.OpenFolderPanel("Select Folder with .unitypackage files", "", "");

        if (string.IsNullOrEmpty(folderPath)) return;

        // Get all package files in the directory
        string[] files = Directory.GetFiles(folderPath, "*.unitypackage", SearchOption.AllDirectories);

        if (files.Length == 0)
        {
            Debug.LogWarning("No .unitypackage files found.");
            return;
        }

        foreach (string file in files)
        {
            Debug.Log($"Importing: {Path.GetFileName(file)}");
            // Set 'false' to skip the manual import dialog for every single package
            AssetDatabase.ImportPackage(file, false);
        }

        AssetDatabase.Refresh();
        Debug.Log("Mass import complete.");
    }
}