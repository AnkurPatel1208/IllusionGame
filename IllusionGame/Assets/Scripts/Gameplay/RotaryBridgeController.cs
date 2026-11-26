using System.Collections;
using System.Collections.Generic;
using Core.Events;
using Grid;
using Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Mechanics {
    /// <summary>
    /// Controls the rotary brass gear bridge with pure Monument Valley puzzle polish:
    /// - Interactive drag rotation via the central brass gear wheel
    /// - Smooth magnetic snapping between:
    ///     0°: Spans horizontally across to the Right Citadel Tower
    ///    90°: Swings back to connect to the Secret Key Balcony
    /// - Automatic pathfinding graph re-linking when snapped
    /// - Prevents rotation while the player is walking
    /// </summary>
    public class RotaryBridgeController : MonoBehaviour {
        public static bool IsAnyDragging { get; private set; }

        [Header("Rotation Target & Pivot")]
        [Tooltip("The root transform of the bridge that rotates around Y axis")]
        [SerializeField] private Transform bridgePivot;
        [Tooltip("Rotary handle/wheel visual that rotates with the bridge")]
        [SerializeField] private Transform rotaryHandle;

        [Header("Rotation Angles")]
        [SerializeField] private float[] snapAngles = new float[] { 0f, 90f };
        [SerializeField] private float currentAngle = 0f;
        [SerializeField] private float targetAngle = 0f;
        [SerializeField] private float snapSmoothTime = 0.20f;
        [SerializeField] private float dragSensitivity = 0.45f;

        [Header("Dynamic PathNode Connections")]
        [Tooltip("PathNodes located on the rotating bridge itself")]
        [SerializeField] private List<PathNode> bridgeNodes = new();
        [Tooltip("Left Tower landing node (the pivot station)")]
        [SerializeField] private PathNode leftIslandNode;
        [Tooltip("Right Tower landing node (reached at 0°)")]
        [SerializeField] private PathNode rightIslandNode;
        [Tooltip("Secret Key Balcony node (reached at 90°)")]
        [SerializeField] private PathNode keyBalconyNode;

        [Header("Wheel Audio & Feedback")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip snapSound;

        private bool isDragging;
        private float angularVelocity;
        private Vector2 lastPointerPos;
        private bool isPointerDown;
        private Camera mainCamera;
        private PlayerController player;
        private int lastConnectedState = -1; // 0 = Right Tower (0°), 1 = Key Balcony (90°)

        public float CurrentAngle => currentAngle;
        public bool IsDragging => isDragging;

        private void Awake() {
            mainCamera = Camera.main;
            player = FindFirstObjectByType<PlayerController>();
            if (bridgePivot == null) bridgePivot = transform;

            currentAngle = targetAngle;
            UpdatePivotRotation(currentAngle);
            ApplyStateConnections(GetStateFromAngle(currentAngle));
        }

        private void OnDisable() {
            if (isDragging) {
                isDragging = false;
                IsAnyDragging = false;
            }
        }

        private void Update() {
            HandlePointerInput();
            ApplyRotation();
        }

        private void HandlePointerInput() {
            if (player == null) player = FindFirstObjectByType<PlayerController>();
            if (player != null) {
                if (player.IsMoving) return;
                if (IsPlayerOnBridge()) return;
            }

            Vector2 currentPointerPos = Vector2.zero;
            bool pointerDownThisFrame = false;
            bool pointerHeld = false;
            bool pointerUpThisFrame = false;

            var touch = Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.isPressed) {
                currentPointerPos = touch.primaryTouch.position.ReadValue();
                pointerDownThisFrame = touch.primaryTouch.press.wasPressedThisFrame;
                pointerHeld = true;
            } else if (touch != null && touch.primaryTouch.press.wasReleasedThisFrame) {
                currentPointerPos = touch.primaryTouch.position.ReadValue();
                pointerUpThisFrame = true;
            } else {
                var mouse = Mouse.current;
                if (mouse != null) {
                    currentPointerPos = mouse.position.ReadValue();
                    pointerDownThisFrame = mouse.leftButton.wasPressedThisFrame;
                    pointerHeld = mouse.leftButton.isPressed;
                    pointerUpThisFrame = mouse.leftButton.wasReleasedThisFrame;
                }
            }

            if (pointerDownThisFrame) {
                if (IsPointerOverUI()) return;

                if (mainCamera == null) mainCamera = Camera.main;
                if (mainCamera != null) {
                    Ray ray = mainCamera.ScreenPointToRay(currentPointerPos);
                    if (Physics.Raycast(ray, out RaycastHit hit, 200f)) {
                        // Check if clicked the rotary wheel or bridge
                        if (hit.transform.IsChildOf(transform) || 
                            (rotaryHandle != null && hit.transform.IsChildOf(rotaryHandle)) ||
                            (bridgePivot != null && hit.transform.IsChildOf(bridgePivot))) {
                            isPointerDown = true;
                            isDragging = true;
                            IsAnyDragging = true;
                            lastPointerPos = currentPointerPos;
                        }
                    }
                }
            }

            if (pointerHeld && isPointerDown && isDragging) {
                float deltaX = currentPointerPos.x - lastPointerPos.x;
                float deltaY = currentPointerPos.y - lastPointerPos.y;
                float delta = Mathf.Abs(deltaX) > Mathf.Abs(deltaY) ? deltaX : deltaY;

                targetAngle = Mathf.Clamp(targetAngle + delta * dragSensitivity, -15f, 105f);
                lastPointerPos = currentPointerPos;
            }

            if (pointerUpThisFrame && isPointerDown) {
                isPointerDown = false;
                isDragging = false;
                IsAnyDragging = false;
                targetAngle = GetNearestSnapAngle(targetAngle);
            }
        }

        private bool IsPointerOverUI() {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        private bool IsPlayerOnBridge() {
            if (player == null || player.CurrentNode == null) return false;
            return bridgeNodes.Contains(player.CurrentNode);
        }

        private void ApplyRotation() {
            float smoothTime = isDragging ? 0.05f : snapSmoothTime;
            currentAngle = Mathf.SmoothDamp(currentAngle, targetAngle, ref angularVelocity, smoothTime);

            if (!isDragging && Mathf.Abs(currentAngle - targetAngle) < 0.2f && Mathf.Abs(angularVelocity) < 0.2f) {
                currentAngle = targetAngle;
                angularVelocity = 0f;
            }

            UpdatePivotRotation(currentAngle);

            int currentState = GetStateFromAngle(currentAngle);
            if (currentState != lastConnectedState) {
                ApplyStateConnections(currentState);
            }
        }

        private void UpdatePivotRotation(float angle) {
            if (bridgePivot != null) {
                // Negative Y rotation swings (2, 0, -2) towards (2, 0, 2)
                bridgePivot.localEulerAngles = new Vector3(0, -angle, 0);
            }
        }

        private int GetStateFromAngle(float angle) {
            float tolerance = 4.5f;
            if (Mathf.Abs(angle - 0f) <= tolerance) return 0;  // Spans to Right Tower
            if (Mathf.Abs(angle - 90f) <= tolerance) return 1; // Swings to Key Balcony
            return -1; // In motion
        }

        private float GetNearestSnapAngle(float fromAngle) {
            float minDiff = float.MaxValue;
            float bestAngle = 0f;

            for (int i = 0; i < snapAngles.Length; i++) {
                float diff = Mathf.Abs(fromAngle - snapAngles[i]);
                if (diff < minDiff) {
                    minDiff = diff;
                    bestAngle = snapAngles[i];
                }
            }
            return bestAngle;
        }

        private void ApplyStateConnections(int state) {
            lastConnectedState = state;
            if (bridgeNodes == null || bridgeNodes.Count == 0) return;

            // Internal bridge connections
            for (int i = 0; i < bridgeNodes.Count; i++) {
                var node = bridgeNodes[i];
                if (node == null) continue;

                if (i > 0 && bridgeNodes[i - 1] != null) {
                    node.AddNeighbor(bridgeNodes[i - 1]);
                    bridgeNodes[i - 1].AddNeighbor(node);
                }
                if (i < bridgeNodes.Count - 1 && bridgeNodes[i + 1] != null) {
                    node.AddNeighbor(bridgeNodes[i + 1]);
                    bridgeNodes[i + 1].AddNeighbor(node);
                }
            }

            PathNode firstBridgeNode = bridgeNodes[0];
            PathNode lastBridgeNode = bridgeNodes[^1];

            // Clean up external connections
            DisconnectExternal(leftIslandNode, firstBridgeNode);
            DisconnectExternal(leftIslandNode, lastBridgeNode);
            DisconnectExternal(rightIslandNode, firstBridgeNode);
            DisconnectExternal(rightIslandNode, lastBridgeNode);
            DisconnectExternal(keyBalconyNode, firstBridgeNode);
            DisconnectExternal(keyBalconyNode, lastBridgeNode);

            if (state == 0) {
                // State 0 (0°): Connects Left Tower to Right Tower across the citadel
                ConnectExternal(leftIslandNode, firstBridgeNode);
                ConnectExternal(rightIslandNode, lastBridgeNode);
            } else if (state == 1) {
                // State 1 (90°): Connects Left Tower to the Secret Key Balcony
                ConnectExternal(leftIslandNode, firstBridgeNode);
                ConnectExternal(keyBalconyNode, lastBridgeNode);
            }
        }

        private void ConnectExternal(PathNode a, PathNode b) {
            if (a != null && b != null) {
                a.AddNeighbor(b);
                b.AddNeighbor(a);
            }
        }

        private void DisconnectExternal(PathNode a, PathNode b) {
            if (a != null && b != null) {
                a.RemoveNeighbor(b);
                b.RemoveNeighbor(a);
            }
        }

        public void SetupBridge(Transform pivot, Transform handle, List<PathNode> nodes, PathNode left, PathNode right, PathNode balcony, float initialAngle = 0f) {
            bridgePivot = pivot;
            rotaryHandle = handle;
            bridgeNodes = nodes;
            leftIslandNode = left;
            rightIslandNode = right;
            keyBalconyNode = balcony;
            currentAngle = initialAngle;
            targetAngle = initialAngle;
            UpdatePivotRotation(currentAngle);
            ApplyStateConnections(GetStateFromAngle(currentAngle));
        }
    }
}
