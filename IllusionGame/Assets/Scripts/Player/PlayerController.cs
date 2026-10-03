using System.Collections;
using System.Collections.Generic;
using Core.Events;
using Grid;
using UnityEngine;

namespace Player {
    /// <summary>
    /// Controls the player character's navigation, movement animations, and puzzle interactions.
    /// Handles path following across normal tiles and optical illusion perspective bridges.
    /// Normalizes movement speed so perspective bridge steps move at uniform visual speed.
    /// </summary>
    public class PlayerController : MonoBehaviour, IEventListener<NodeClickedEvent>, IEventListener<ResetLevelEvent> {
        [Header("Starting State")]
        [SerializeField] private PathNode currentNode;
        [SerializeField] private PathNode initialSpawnNode;

        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 3.2f;
        [SerializeField] private float turnSpeed = 12f;
        [SerializeField] private float stepBobHeight = 0.12f;
        [SerializeField] private float bobFrequency = 2f;

        [Header("Visual Avatar")]
        [SerializeField] private Transform visualRoot;

        private bool isMoving;
        private Coroutine moveCoroutine;
        private Vector3 originalVisualLocalPos;

        public PathNode CurrentNode => currentNode;
        public bool IsMoving => isMoving;

        private void Awake() {
            if (visualRoot != null) {
                originalVisualLocalPos = visualRoot.localPosition;
            }
        }

        private void OnEnable() {
            EventBus<NodeClickedEvent>.Subscribe(this);
            EventBus<ResetLevelEvent>.Subscribe(this);
        }

        private void OnDisable() {
            EventBus<NodeClickedEvent>.Unsubscribe(this);
            EventBus<ResetLevelEvent>.Unsubscribe(this);
        }

        private void Start() {
            if (initialSpawnNode != null && currentNode == null) {
                SetCurrentNode(initialSpawnNode);
            } else if (currentNode != null) {
                transform.position = currentNode.WalkPosition;
            }
        }

        public void SetCurrentNode(PathNode node) {
            currentNode = node;
            if (initialSpawnNode == null) {
                initialSpawnNode = node;
            }
            if (currentNode != null) {
                transform.position = currentNode.WalkPosition;
            }
        }

        public void OnEventRaised(NodeClickedEvent eventData) {
            if (isMoving) return;
            if (eventData.Node == null || eventData.Node == currentNode) return;

            var path = Pathfinder.FindPath(currentNode, eventData.Node);
            if (path != null && path.Count > 1) {
                StartMovement(path);
            }
        }

        public void OnEventRaised(ResetLevelEvent eventData) {
            if (moveCoroutine != null) {
                StopCoroutine(moveCoroutine);
                moveCoroutine = null;
            }
            isMoving = false;
            if (initialSpawnNode != null) {
                SetCurrentNode(initialSpawnNode);
            }
        }

        private void StartMovement(List<PathNode> path) {
            if (moveCoroutine != null) {
                StopCoroutine(moveCoroutine);
            }
            moveCoroutine = StartCoroutine(FollowPathRoutine(path));
        }

        private IEnumerator FollowPathRoutine(List<PathNode> path) {
            isMoving = true;
            EventBus<PlayerMoveStartedEvent>.Raise(new PlayerMoveStartedEvent(currentNode, path[^1]));

            // Path index 0 is currentNode, start moving from index 1
            for (int i = 1; i < path.Count; i++) {
                var targetNode = path[i];
                yield return StartCoroutine(MoveToNodeRoutine(targetNode));
                currentNode = targetNode;
                EventBus<PlayerNodeReachedEvent>.Raise(new PlayerNodeReachedEvent(currentNode));
            }

            if (visualRoot != null) {
                visualRoot.localPosition = originalVisualLocalPos;
            }

            isMoving = false;
            moveCoroutine = null;
            EventBus<PlayerMoveCompletedEvent>.Raise(new PlayerMoveCompletedEvent(currentNode));
        }

        private IEnumerator MoveToNodeRoutine(PathNode targetNode) {
            Vector3 startPos = transform.position;
            Vector3 targetPos = targetNode.WalkPosition;

            // Check if this step traverses an optical illusion perspective edge
            bool isPerspectiveStep = currentNode != null && currentNode.HasActivePerspectiveEdgeTo(targetNode, out _);

            // In an optical illusion, 3D displacement along the camera forward ray is imperceptible on screen.
            // On screen, the perspective bridge is exactly 1 tile away, so effective visual distance is 1.0f.
            // This prevents the character from moving in slow-motion across the bridge.
            float effectiveDistance = isPerspectiveStep ? 1.0f : Vector3.Distance(startPos, targetPos);
            float duration = effectiveDistance / Mathf.Max(0.1f, moveSpeed);
            float elapsed = 0f;

            // Rotation towards target:
            // For perspective steps, orient along the visual horizontal movement direction
            Vector3 moveDirection;
            Camera cam = Camera.main;
            if (isPerspectiveStep && cam != null) {
                Vector3 camFwd = cam.transform.forward;
                Vector3 screenDelta = (targetPos - startPos) - Vector3.Dot(targetPos - startPos, camFwd) * camFwd;
                // Match screen delta to the best horizontal cardinal direction (+X, -X, +Z, -Z)
                Vector3 bestCardinal = Vector3.forward;
                float bestDot = float.MinValue;
                Vector3[] cardinals = { Vector3.right, Vector3.left, Vector3.forward, Vector3.back };
                for (int c = 0; c < cardinals.Length; c++) {
                    Vector3 cardProj = cardinals[c] - Vector3.Dot(cardinals[c], camFwd) * camFwd;
                    float dot = Vector3.Dot(screenDelta.normalized, cardProj.normalized);
                    if (dot > bestDot) {
                        bestDot = dot;
                        bestCardinal = cardinals[c];
                    }
                }
                moveDirection = bestCardinal;
            } else {
                moveDirection = (targetPos - startPos);
                moveDirection.y = 0;
            }

            if (moveDirection.sqrMagnitude > 0.001f) {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection.normalized, Vector3.up);
                StartCoroutine(SmoothRotate(targetRotation, 1f / Mathf.Max(0.1f, turnSpeed)));
            }

            while (elapsed < duration) {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // World position interpolation
                transform.position = Vector3.Lerp(startPos, targetPos, t);

                // Step bobbing animation
                if (visualRoot != null) {
                    float bob = Mathf.Sin(t * Mathf.PI * bobFrequency) * stepBobHeight;
                    visualRoot.localPosition = originalVisualLocalPos + new Vector3(0, Mathf.Abs(bob), 0);
                }

                yield return null;
            }

            transform.position = targetPos;
        }

        private IEnumerator SmoothRotate(Quaternion targetRotation, float duration) {
            Quaternion startRot = transform.rotation;
            float elapsed = 0f;
            while (elapsed < duration) {
                elapsed += Time.deltaTime;
                transform.rotation = Quaternion.Slerp(startRot, targetRotation, elapsed / duration);
                yield return null;
            }
            transform.rotation = targetRotation;
        }
    }
}
