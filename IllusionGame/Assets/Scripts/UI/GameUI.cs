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
            Color originalColor = GUI.color;

            if (levelManager == null) {
                levelManager = FindFirstObjectByType<LevelManager>();
            }
            if (cameraController == null) {
                var cam = Camera.main;
                if (cam != null) cameraController = cam.GetComponent<IsometricCameraController>();
            }

            string currentLevelTitle = (levelManager != null && !string.IsNullOrEmpty(levelManager.LevelName))
                ? levelManager.LevelName.ToUpper()
                : "LEVEL 2 – THE ROTARY CITADEL";

            string objectiveText;
            if (currentLevelTitle.Contains("LEVEL 2") || currentLevelTitle.Contains("ROTARY")) {
                objectiveText = !hasKey
                    ? "Turn the gear wheel to reach the key, then align the summit path."
                    : "Key collected! Ascend to the summit portal.";
            } else {
                objectiveText = !hasKey
                    ? "Rotate camera to align platforms, then reach the key."
                    : "Key collected! Cross back and enter the illuminated door.";
            }

            bool isPortrait = Screen.width < Screen.height;

            if (isPortrait) {
                // Portrait Layout
                titleStyle.alignment = TextAnchor.UpperCenter;
                subtitleStyle.alignment = TextAnchor.UpperCenter;

                float headerWidth = Mathf.Min(Screen.width - 40, 500);
                GUILayout.BeginArea(new Rect((Screen.width - headerWidth) * 0.5f, 20, headerWidth, 75));
                GUILayout.Label("ILLUSION", titleStyle);
                GUILayout.Label(currentLevelTitle, subtitleStyle);
                GUILayout.Label(objectiveText, subtitleStyle);
                GUILayout.EndArea();

                float badgeWidth = Mathf.Min(Screen.width - 50, 240);
                float badgeHeight = 40;
                Rect badgeRect = new Rect((Screen.width - badgeWidth) * 0.5f, 100, badgeWidth, badgeHeight);

                originalColor = GUI.color;
                if (isPerspectiveAligned) {
                    GUI.color = new Color(0.2f, 0.85f, 0.35f, 0.95f);
                    GUI.Box(badgeRect, "✔  ALIGNED! Path is open", statusBadgeStyle);
                }
                GUI.color = originalColor;

                if (hasKey) {
                    Rect keyRect = new Rect((Screen.width - badgeWidth) * 0.5f, 146, badgeWidth, 32);
                    GUI.color = new Color(1f, 0.85f, 0.2f, 0.95f);
                    GUI.Box(keyRect, "★  KEY COLLECTED", statusBadgeStyle);
                    GUI.color = originalColor;
                }
            } else {
                // Landscape Layout
                titleStyle.alignment = TextAnchor.UpperLeft;
                subtitleStyle.alignment = TextAnchor.UpperLeft;

                GUILayout.BeginArea(new Rect(25, 20, 520, 100));
                GUILayout.Label("ILLUSION", titleStyle);
                GUILayout.Label(currentLevelTitle, subtitleStyle);
                GUILayout.Label(objectiveText, subtitleStyle);
                GUILayout.EndArea();

                float badgeWidth = 220;
                float badgeHeight = 55;
                Rect badgeRect = new Rect(Screen.width - badgeWidth - 25, 25, badgeWidth, badgeHeight);

                originalColor = GUI.color;
                if (isPerspectiveAligned) {
                    GUI.color = new Color(0.2f, 0.85f, 0.35f, 0.95f);
                    GUI.Box(badgeRect, "✔  ALIGNED!\nPath is open", statusBadgeStyle);
                }
                GUI.color = originalColor;

                if (hasKey) {
                    Rect keyRect = new Rect(Screen.width - badgeWidth - 25, 90, badgeWidth, 35);
                    GUI.color = new Color(1f, 0.85f, 0.2f, 0.95f);
                    GUI.Box(keyRect, "★  KEY COLLECTED", statusBadgeStyle);
                    GUI.color = originalColor;
                }
            }

            // Camera Controls (Bottom Center)
            float controlWidth = Mathf.Min(Screen.width - 40, 360);
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

            // Reset Button (Bottom Left)
            Rect resetRect = new Rect(20, Screen.height - 55, 90, 35);
            if (GUI.Button(resetRect, "↺ Reset", buttonStyle)) {
                if (levelManager != null) levelManager.RestartLevel();
            }

            // Completion Modal Overlay
            if (isLevelCompleted) {
                GUI.color = new Color(0f, 0f, 0f, 0.65f);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
                GUI.color = originalColor;

                float modalWidth = Mathf.Min(Screen.width - 60, 420);
                float modalHeight = 220;
                Rect modalRect = new Rect((Screen.width - modalWidth) * 0.5f, (Screen.height - modalHeight) * 0.5f, modalWidth, modalHeight);

                GUI.color = new Color(0.12f, 0.16f, 0.22f, 0.95f);
                GUI.Box(modalRect, GUIContent.none, modalBoxStyle);
                GUI.color = originalColor;

                GUILayout.BeginArea(new Rect(modalRect.x + 20, modalRect.y + 20, modalWidth - 40, modalHeight - 40));
                GUILayout.Label("LEVEL COMPLETE", completionTitleStyle);
                GUILayout.Space(12);

                GUIStyle msgStyle = new GUIStyle(GUI.skin.label) {
                    fontSize = 15,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.9f, 0.95f, 1f, 0.95f) }
                };
                GUILayout.Label("You solved the optical mystery and reached the summit sanctuary!", msgStyle);
                GUILayout.Space(20);

                if (GUILayout.Button("Replay Level", buttonStyle, GUILayout.Height(42))) {
                    if (levelManager != null) levelManager.RestartLevel();
                }

                GUILayout.EndArea();
            }
        }
    }
}
