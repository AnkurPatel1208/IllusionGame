using Core.Events;
using CameraControl;
using Gameplay;
using UnityEngine;

namespace UI {
    /// <summary>
    /// Displays game HUD, alignment status, rotation controls, and level completion screen.
    /// Provides immediate GUI rendering and event integration.
    /// </summary>
    public class GameUI : MonoBehaviour,
        IEventListener<PerspectiveAlignmentChangedEvent>,
        IEventListener<KeyCollectedEvent>,
        IEventListener<LevelCompletedEvent>,
        IEventListener<ResetLevelEvent> {

        [Header("References")]
        [SerializeField] private IsometricCameraController cameraController;
        [SerializeField] private LevelManager levelManager;

        [Header("State")]
        private bool isPerspectiveAligned;
        private bool hasKey;
        private bool isLevelCompleted;

        private GUIStyle titleStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle statusBadgeStyle;
        private GUIStyle buttonStyle;
        private GUIStyle modalBoxStyle;
        private GUIStyle completionTitleStyle;
        private bool stylesInitialized;

        private void OnEnable() {
            EventBus<PerspectiveAlignmentChangedEvent>.Subscribe(this);
            EventBus<KeyCollectedEvent>.Subscribe(this);
            EventBus<LevelCompletedEvent>.Subscribe(this);
            EventBus<ResetLevelEvent>.Subscribe(this);
        }

        private void OnDisable() {
            EventBus<PerspectiveAlignmentChangedEvent>.Unsubscribe(this);
            EventBus<KeyCollectedEvent>.Unsubscribe(this);
            EventBus<LevelCompletedEvent>.Unsubscribe(this);
            EventBus<ResetLevelEvent>.Unsubscribe(this);
        }

        public void Initialize(IsometricCameraController cam, LevelManager manager) {
            cameraController = cam;
            levelManager = manager;
        }

        public void OnEventRaised(PerspectiveAlignmentChangedEvent eventData) {
            isPerspectiveAligned = eventData.IsAligned;
        }

        public void OnEventRaised(KeyCollectedEvent eventData) {
            hasKey = true;
        }

        public void OnEventRaised(LevelCompletedEvent eventData) {
            isLevelCompleted = true;
        }

        public void OnEventRaised(ResetLevelEvent eventData) {
            isPerspectiveAligned = false;
            hasKey = false;
            isLevelCompleted = false;
        }

        private void InitStyles() {
            if (stylesInitialized) return;

            titleStyle = new GUIStyle(GUI.skin.label) {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperLeft,
                normal = { textColor = new Color(0.95f, 0.95f, 1f, 1f) }
            };

            subtitleStyle = new GUIStyle(GUI.skin.label) {
                fontSize = 14,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.UpperLeft,
                normal = { textColor = new Color(0.75f, 0.8f, 0.9f, 0.9f) }
            };

            statusBadgeStyle = new GUIStyle(GUI.skin.box) {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            buttonStyle = new GUIStyle(GUI.skin.button) {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            modalBoxStyle = new GUIStyle(GUI.skin.box) {
                alignment = TextAnchor.MiddleCenter
            };

            completionTitleStyle = new GUIStyle(GUI.skin.label) {
                fontSize = 26,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 0.85f, 0.3f, 1f) }
            };

            stylesInitialized = true;
        }

        private void OnGUI() {
            InitStyles();

            // 1. Top Header
            GUILayout.BeginArea(new Rect(25, 20, 450, 100));
            GUILayout.Label("ILLUSION", titleStyle);
            GUILayout.Label("LEVEL 1 – THE PERSPECTIVE BRIDGE", subtitleStyle);
            string objectiveText = !hasKey 
                ? "Rotate camera to align platforms, then reach the key."
                : "Key collected! Cross back and enter the illuminated door.";
            GUILayout.Label(objectiveText, subtitleStyle);
            GUILayout.EndArea();

            // 2. Alignment Status Badge (Right side, matching the reference image)
            float badgeWidth = 210;
            float badgeHeight = 55;
            Rect badgeRect = new Rect(Screen.width - badgeWidth - 25, 25, badgeWidth, badgeHeight);

            Color originalColor = GUI.color;
            if (isPerspectiveAligned) {
                GUI.color = new Color(0.2f, 0.85f, 0.35f, 0.95f);
                GUI.Box(badgeRect, "✔  ALIGNED!\nWalk across the bridge", statusBadgeStyle);
            } else {
                GUI.color = new Color(0.9f, 0.25f, 0.25f, 0.95f);
                GUI.Box(badgeRect, "✖  NOT CONNECTED\nRotate camera to align", statusBadgeStyle);
            }
            GUI.color = originalColor;

            // 3. Key Inventory Indicator
            if (hasKey) {
                Rect keyRect = new Rect(Screen.width - badgeWidth - 25, 90, badgeWidth, 35);
                GUI.color = new Color(1f, 0.85f, 0.2f, 0.95f);
                GUI.Box(keyRect, "★  KEY COLLECTED", statusBadgeStyle);
                GUI.color = originalColor;
            }

            // 4. Camera Controls (Bottom Center)
            float controlWidth = 360;
            float controlHeight = 55;
            Rect controlRect = new Rect((Screen.width - controlWidth) * 0.5f, Screen.height - controlHeight - 20, controlWidth, controlHeight);
            GUILayout.BeginArea(controlRect);
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("◀ Rotate Left (Q)", buttonStyle, GUILayout.Height(45))) {
                if (cameraController != null) cameraController.RotateToPreviousSnap();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Rotate Right (E) ▶", buttonStyle, GUILayout.Height(45))) {
                if (cameraController != null) cameraController.RotateToNextSnap();
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            // 5. Reset button (Bottom Left)
            if (GUI.Button(new Rect(25, Screen.height - 50, 110, 35), "↺ Reset", buttonStyle)) {
                if (levelManager != null) {
                    levelManager.RestartLevel();
                } else {
                    EventBus<ResetLevelEvent>.Raise(new ResetLevelEvent());
                }
            }

            // 6. Level Complete Modal
            if (isLevelCompleted) {
                float modalWidth = 380;
                float modalHeight = 200;
                Rect modalRect = new Rect((Screen.width - modalWidth) * 0.5f, (Screen.height - modalHeight) * 0.5f, modalWidth, modalHeight);

                GUI.color = new Color(0.1f, 0.12f, 0.16f, 0.97f);
                GUI.Box(modalRect, GUIContent.none, modalBoxStyle);
                GUI.color = originalColor;

                GUILayout.BeginArea(modalRect);
                GUILayout.Space(25);
                GUILayout.Label("LEVEL COMPLETE!", completionTitleStyle);
                GUILayout.Space(10);
                GUILayout.Label("Reality is Perspective.", subtitleStyle);
                GUILayout.Space(25);

                if (GUILayout.Button("Play Again", buttonStyle, GUILayout.Height(45))) {
                    if (levelManager != null) {
                        levelManager.RestartLevel();
                    } else {
                        EventBus<ResetLevelEvent>.Raise(new ResetLevelEvent());
                    }
                }
                GUILayout.EndArea();
            }
        }
    }
}
