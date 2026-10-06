using System.Collections.Generic;
using Core.Events;
using UnityEngine;

namespace CameraControl {
    /// <summary>
    /// Monitors camera rotation and automatically fades or hides architectural elements
    /// that would obstruct the player's view of the puzzle path at reverse angles (e.g. 225 deg).
    /// </summary>
    public class CameraOcclusionManager : MonoBehaviour, IEventListener<CameraRotationChangedEvent> {
        [Header("Obstruction Groups")]
        [Tooltip("Objects that should fade/hide when viewing from the back (Yaw around 225 degrees)")]
        [SerializeField] private List<Renderer> backViewOccluders = new List<Renderer>();

        [Header("Fade Settings")]
        [SerializeField] private float fadeDuration = 0.25f;
        [SerializeField] private float minAlpha = 0.20f;

        private float currentYaw = 45f;
        private bool isBackView;

        private void OnEnable() {
            EventBus<CameraRotationChangedEvent>.Subscribe(this);
        }

        private void OnDisable() {
            EventBus<CameraRotationChangedEvent>.Unsubscribe(this);
        }

        public void RegisterBackViewOccluder(Renderer rend) {
            if (rend != null && !backViewOccluders.Contains(rend)) {
                backViewOccluders.Add(rend);
            }
        }

        public void ClearOccluders() {
            backViewOccluders.Clear();
        }

        public void OnEventRaised(CameraRotationChangedEvent eventData) {
            currentYaw = eventData.CurrentYaw;
            UpdateOcclusion();
        }

        private void Update() {
            UpdateOcclusion();
        }

        public void UpdateOcclusion() {
            // When camera is looking from the back (Yaw around 225 deg, or between 180 and 270)
            float normYaw = (currentYaw % 360f + 360f) % 360f;
            bool shouldHide = normYaw > 170f && normYaw < 280f;

            if (shouldHide != isBackView) {
                isBackView = shouldHide;
            }

            // Smoothly enable/disable or fade occluders
            for (int i = backViewOccluders.Count - 1; i >= 0; i--) {
                var rend = backViewOccluders[i];
                if (rend == null) {
                    backViewOccluders.RemoveAt(i);
                    continue;
                }
                rend.enabled = !isBackView;
            }
        }
    }
}
