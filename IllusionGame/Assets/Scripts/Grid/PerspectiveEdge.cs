using Core.Events;
using UnityEngine;

namespace Grid {
    /// <summary>
    /// Represents an optical illusion bridge connection between two physically disconnected nodes.
    /// Only active and walkable when the camera is oriented at the required perspective angle.
    /// </summary>
    public class PerspectiveEdge : MonoBehaviour, IEventListener<PerspectiveAlignmentChangedEvent> {
        [Header("Connected Nodes")]
        [SerializeField] private PathNode nodeA;
        [SerializeField] private PathNode nodeB;

        [Header("Alignment Settings")]
        [Tooltip("Target camera yaw angle where the optical illusion aligns (0 - 360)")]
        [SerializeField] private float targetYaw = 45f;
        [Tooltip("Allowed angle difference to consider the perspective aligned (in degrees)")]
        [SerializeField] private float angleTolerance = 3f;

        [Header("State")]
        [SerializeField] private bool isActive;

        public PathNode NodeA => nodeA;
        public PathNode NodeB => nodeB;
        public float TargetYaw => targetYaw;
        public float AngleTolerance => angleTolerance;
        public bool IsActive => isActive;

        private void OnEnable() {
            EventBus<PerspectiveAlignmentChangedEvent>.Subscribe(this);
            RegisterToNodes();
        }

        private void OnDisable() {
            EventBus<PerspectiveAlignmentChangedEvent>.Unsubscribe(this);
            UnregisterFromNodes();
        }

        public void Initialize(PathNode a, PathNode b, float yaw, float tolerance = 3f) {
            nodeA = a;
            nodeB = b;
            targetYaw = yaw;
            angleTolerance = tolerance;
            RegisterToNodes();
        }

        private void RegisterToNodes() {
            if (nodeA != null) nodeA.RegisterPerspectiveEdge(this);
            if (nodeB != null) nodeB.RegisterPerspectiveEdge(this);
        }

        private void UnregisterFromNodes() {
            if (nodeA != null) nodeA.UnregisterPerspectiveEdge(this);
            if (nodeB != null) nodeB.UnregisterPerspectiveEdge(this);
        }

        public PathNode GetOtherNode(PathNode current) {
            if (current == nodeA) return nodeB;
            if (current == nodeB) return nodeA;
            return null;
        }

        public void OnEventRaised(PerspectiveAlignmentChangedEvent eventData) {
            // Check if this event's target angle matches our target angle and is within tolerance
            float diff = Mathf.Abs(Mathf.DeltaAngle(eventData.TargetAngle, targetYaw));
            if (diff < 1f) {
                SetActiveState(eventData.IsAligned);
            }
        }

        public void SetActiveState(bool active) {
            if (isActive == active) return;
            isActive = active;
        }

        /// <summary>
        /// Direct evaluation of alignment given a camera yaw.
        /// </summary>
        public bool CheckAlignment(float cameraYaw) {
            float diff = Mathf.Abs(Mathf.DeltaAngle(cameraYaw, targetYaw));
            bool aligned = diff <= angleTolerance;
            SetActiveState(aligned);
            return aligned;
        }

        private void OnDrawGizmos() {
            if (nodeA == null || nodeB == null) return;

            Gizmos.color = isActive ? Color.green : new Color(1f, 0.5f, 0f, 0.4f);
            Gizmos.DrawLine(nodeA.WalkPosition, nodeB.WalkPosition);

            // Draw midpoint indicator
            Vector3 mid = (nodeA.WalkPosition + nodeB.WalkPosition) * 0.5f;
            Gizmos.DrawWireCube(mid, Vector3.one * 0.2f);
        }
    }
}
