#if UNITY_EDITOR
using System;
using System.IO;
using Level;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EditorTools {
    [InitializeOnLoad]
    public static class Level1EditorMenu {
        static Level1EditorMenu() {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
        }

        private static void OnEditorUpdate() {
            string triggerFile = "Temp/BakeLevel1Trigger.txt";
            if (File.Exists(triggerFile)) {
                try {
                    File.Delete(triggerFile);
                } catch { }

                Debug.Log("<color=cyan>[Level1EditorMenu]</color> Trigger detected in Editor update! Baking Level 1 now...");
                BuildLevel1Scene();
            }
        }

        [MenuItem("Illusion Game/Build Level 1 – The Perspective Bridge (Dungeon) %&l")]
        public static void BuildLevel1Scene() {
            string logPath = "/tmp/bake_debug.log";
            try {
                File.WriteAllText(logPath, $"[Start] BuildLevel1Scene called at {DateTime.Now}\n");

                var activeScene = EditorSceneManager.GetActiveScene();
                File.AppendAllText(logPath, $"Active scene path: {activeScene.path}\n");

                if (activeScene.path != "Assets/Scenes/SampleScene.unity" && File.Exists("Assets/Scenes/SampleScene.unity")) {
                    activeScene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
                    File.AppendAllText(logPath, $"Opened scene: {activeScene.path}\n");
                }

                var builderObj = GameObject.Find("Level1_Builder");
                if (builderObj == null) {
                    builderObj = new GameObject("Level1_Builder");
                    Undo.RegisterCreatedObjectUndo(builderObj, "Create Level1_Builder");
                }
                File.AppendAllText(logPath, $"builderObj found/created: {builderObj.name}\n");

                var builder = builderObj.GetComponent<Level1Builder>();
                if (builder == null) {
                    builder = builderObj.AddComponent<Level1Builder>();
                }
                File.AppendAllText(logPath, $"builder component obtained\n");

                builder.AutoAssignDungeonAssets();
                File.AppendAllText(logPath, "AutoAssignDungeonAssets completed\n");

                builder.BuildOnStart = false; // Do not instantiate at runtime!
                builder.BuildLevel();
                File.AppendAllText(logPath, "BuildLevel completed\n");

                Selection.activeGameObject = builderObj;
                EditorUtility.SetDirty(builderObj);
                EditorSceneManager.MarkSceneDirty(activeScene);
                bool saved = EditorSceneManager.SaveScene(activeScene);
                AssetDatabase.SaveAssets();

                string msg = $"Level 1 successfully baked as persistent scene objects into SampleScene.unity (Saved: {saved}) with buildOnStart = false!";
                File.AppendAllText(logPath, $"SUCCESS: {msg}\n");
                Debug.Log($"<color=green>[Level1EditorMenu]</color> {msg}");
                CaptureScreenshot();
            } catch (Exception ex) {
                File.AppendAllText(logPath, $"EXCEPTION: {ex}\n");
                Debug.LogError($"[Level1EditorMenu] Error: {ex}");
            }
        }

        [MenuItem("Illusion Game/Capture Level 1 Screenshot")]
        public static void CaptureScreenshot() {
            var cam = Camera.main;
            if (cam == null) cam = UnityEngine.Object.FindAnyObjectByType<Camera>();
            if (cam != null) {
                int width = 1280;
                int height = 720;
                var rt = new RenderTexture(width, height, 24);
                var prevRt = cam.targetTexture;
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();
                cam.targetTexture = prevRt;
                RenderTexture.active = null;
                UnityEngine.Object.DestroyImmediate(rt);

                byte[] bytes = tex.EncodeToPNG();
                UnityEngine.Object.DestroyImmediate(tex);
                File.WriteAllBytes("screenshot_level1.png", bytes);
                Debug.Log("<color=green>[Level1EditorMenu]</color> Screenshot captured to screenshot_level1.png");
            }
        }
    }
}
#endif
