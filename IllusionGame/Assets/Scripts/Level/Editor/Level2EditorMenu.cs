#if UNITY_EDITOR
using System;
using System.IO;
using Level;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EditorTools {
    [InitializeOnLoad]
    public static class Level2EditorMenu {
        static Level2EditorMenu() {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
        }

        private static void OnEditorUpdate() {
            string triggerFile = "Temp/BakeLevel2Trigger.txt";
            if (File.Exists(triggerFile)) {
                if (EditorApplication.isCompiling || EditorApplication.isUpdating) {
                    return;
                }

                try {
                    File.Delete(triggerFile);
                } catch { }

                Debug.Log("<color=cyan>[Level2EditorMenu]</color> Trigger detected in Editor update! Baking Level 2 now...");
                BuildLevel2Scene();
            }

            string screenTrigger = "Temp/CaptureLevel2ScreenshotTrigger.txt";
            if (File.Exists(screenTrigger)) {
                if (EditorApplication.isCompiling || EditorApplication.isUpdating) {
                    return;
                }
                try { File.Delete(screenTrigger); } catch { }
                CaptureLevel2Screenshot();
            }
        }

        [MenuItem("Illusion Game/Build Level 2 – The Rotary Crossing")]
        public static void BuildLevel2Scene() {
            string scenePath = "Assets/Scenes/Level2.unity";
            try {
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                // Open or create Level 2 scene
                UnityEngine.SceneManagement.Scene level2Scene;
                if (File.Exists(scenePath)) {
                    level2Scene = EditorSceneManager.OpenScene(scenePath);
                } else {
                    level2Scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                    EditorSceneManager.SaveScene(level2Scene, scenePath);
                }

                // Find or create Level2_Builder
                var builderObj = GameObject.Find("Level2_Builder");
                if (builderObj == null) {
                    builderObj = new GameObject("Level2_Builder");
                    Undo.RegisterCreatedObjectUndo(builderObj, "Create Level2_Builder");
                }

                var builder = builderObj.GetComponent<Level2Builder>();
                if (builder == null) {
                    builder = builderObj.AddComponent<Level2Builder>();
                }

                // Build level geometry
                builder.BuildLevel();

                // Save scene
                EditorSceneManager.MarkSceneDirty(level2Scene);
                EditorSceneManager.SaveScene(level2Scene, scenePath);

                Debug.Log($"<color=green>[Level2EditorMenu]</color> Level 2 baked successfully into {scenePath}!");
            } catch (Exception ex) {
                Debug.LogError($"[Level2EditorMenu] Failed to build Level 2: {ex.Message}\n{ex.StackTrace}");
            }
        }

        [MenuItem("Illusion Game/Capture Level 2 Screenshots")]
        public static void CaptureLevel2Screenshot() {
            string scenePath = "Assets/Scenes/Level2.unity";
            if (EditorSceneManager.GetActiveScene().path != scenePath) {
                if (File.Exists(scenePath)) {
                    EditorSceneManager.OpenScene(scenePath);
                }
            }

            var builder = GameObject.Find("Level2_Builder")?.GetComponent<Level2Builder>();
            if (builder != null) {
                builder.BuildLevel();
            }

            Camera cam = Camera.main;
            if (cam == null) return;

            RenderCameraToFile(cam, 1080, 1920, "screenshot_level2.png");
            RenderCameraToFile(cam, 1920, 1080, "screenshot_level2_landscape.png");
        }

        private static void RenderCameraToFile(Camera cam, int width, int height, string outPath) {
            RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            cam.targetTexture = null;
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(rt);

            byte[] bytes = tex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(tex);

            File.WriteAllBytes(outPath, bytes);
            Debug.Log($"<color=green>[Level2EditorMenu]</color> Captured screenshot at {outPath}");
        }
    }
}
#endif
