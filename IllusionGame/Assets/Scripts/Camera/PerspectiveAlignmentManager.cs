using Core.Events;
using UnityEngine;

namespace CameraControl {
    /// <summary>
    /// Monitors camera rotation and detects when perspective optical illusions are aligned.
    /// Broadcasts alignment events to update the puzzle graph and UI.
    /// </summary>
    public class PerspectiveAlignmentManager : MonoBehaviour, IEventListener<CameraRotationChangedEvent> {
        [Header("Alignment Configuration")]
        [Tooltip("The camera yaw angle that creates the optical bridge illusion (e.g. 45 degrees)")]
        [SerializeField] private float illusionYawAngle = 45f;
        [Tooltip("Threshold in degrees around the target angle to consider it aligned")]
        [SerializeField] private float toleranceAngle = 3f;

        [Header("Status")]
        [SerializeField] private bool isAligned;

        public bool IsAligned => isAligned;
        public float IllusionAngle => illusionYawAngle;

        private void OnEnable() {
            EventBus<CameraRotationChangedEvent>.Subscribe(this);
        }

        private void OnDisable() {
            EventBus<CameraRotationChangedEvent>.Unsubscribe(this);
        }

        public void Configure(float targetAngle, float tolerance = 3f) {
            illusionYawAngle = targetAngle;
            toleranceAngle = tolerance;
        }

        public void OnEventRaised(CameraRotationChangedEvent eventData) {
            EvaluateAlignment(eventData.CurrentYaw);
        }

        public void EvaluateAlignment(float currentYaw) {
            float diff = Mathf.Abs(Mathf.DeltaAngle(currentYaw, illusionYawAngle));
            bool currentlyAligned = diff <= toleranceAngle;

            if (currentlyAligned != isAligned) {
                isAligned = currentlyAligned;
                EventBus<PerspectiveAlignmentChangedEvent>.Raise(
                    new PerspectiveAlignmentChangedEvent(isAligned, illusionYawAngle, diff)
                );
            }
        }
    }
}
