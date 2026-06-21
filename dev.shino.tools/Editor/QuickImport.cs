using UnityEditor;

namespace Shino.Tools.Editor
{
    public class QuickImport {
        [MenuItem("Tools/Shino/Quick Import")]
        public static void Import() {
            string path = EditorUtility.OpenFilePanel("Import Package", "", "unitypackage");
            if (!string.IsNullOrEmpty(path)) AssetDatabase.ImportPackage(path, true);
        }
    }
}