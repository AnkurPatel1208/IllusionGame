#if UNITY_EDITOR
using Level;
using UnityEditor;
using UnityEngine;

namespace EditorTools {
    public static class Level1EditorMenu {
        [MenuItem("Illusion Game/Build Level 1 – The Perspective Bridge")]
        public static void BuildLevel1Scene() {
            var builderObj = GameObject.Find("Level1_Builder");
            if (builderObj == null) {
                builderObj = new GameObject("Level1_Builder");
                Undo.RegisterCreatedObjectUndo(builderObj, "Create Level1_Builder");
            }

            var builder = builderObj.GetComponent<Level1Builder>();
            if (builder == null) {
                builder = builderObj.AddComponent<Level1Builder>();
            }

            builder.BuildLevel();
            Selection.activeGameObject = builderObj;
            EditorUtility.SetDirty(builderObj);
        }
    }
}
#endif
