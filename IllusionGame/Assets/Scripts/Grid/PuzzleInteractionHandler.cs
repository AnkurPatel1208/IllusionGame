using CameraControl;
using Core.Events;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Grid {
    /// <summary>
    /// Handles user pointer interactions (hover and click) on puzzle tiles using the New Input System.
    /// Distinguishes between clean taps/clicks (navigating) and drags/swipes (rotating camera).
    /// Safely handles UI blocking and Raycasting.
    /// </summary>
    public class PuzzleInteractionHandler : MonoBehaviour {
        [Header("Settings")]
        [SerializeField] private LayerMask tileLayerMask = ~0;
        [SerializeField] private float rayDistance = 100f;
        [SerializeField] private float tapThresholdPixels = 12f;

        private Camera mainCam;
        private IsometricCameraController camController;
        private PathNode currentHoveredNode;

        // Press / Tap detection
        private bool isPointerPressed;
        private Vector2 pointerDownPos;
        private PathNode potentialTargetNode;
        private int activeTouchId = -1;

        private void Awake() {
            mainCam = Camera.main;
            if (mainCam != null) {
                camController = mainCam.GetComponent<IsometricCameraController>();
            }
        }

        private void Update() {
            if (mainCam == null) {
                mainCam = Camera.main;
                if (mainCam != null) {
                    camController = mainCam.GetComponent<IsometricCameraController>();
                }
                if (mainCam == null) return;
            }

            Vector2 pointerPos = Vector2.zero;
            bool hasPointer = false;
            bool pointerDown = false;
            bool pointerHeld = false;
            bool pointerUp = false;
            int touchId = -1;

            // 1. Mobile Touch Input
            var touch = Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.isPressed) {
                touchId = touch.primaryTouch.touchId.ReadValue();
                pointerPos = touch.primaryTouch.position.ReadValue();
                hasPointer = true;
                pointerDown = touch.primaryTouch.press.wasPressedThisFrame;
                pointerHeld = true;
            } else if (touch != null && touch.primaryTouch.press.wasReleasedThisFrame) {
                touchId = touch.primaryTouch.touchId.ReadValue();
                pointerPos = touch.primaryTouch.position.ReadValue();
                hasPointer = true;
                pointerUp = true;
            }
            // 2. PC Mouse Input
            else {
                var mouse = Mouse.current;
                if (mouse != null) {
                    pointerPos = mouse.position.ReadValue();
                    hasPointer = true;
                    pointerDown = mouse.leftButton.wasPressedThisFrame;
                    pointerHeld = mouse.leftButton.isPressed;
                    pointerUp = mouse.leftButton.wasReleasedThisFrame;
                }
            }

            if (!hasPointer) {
                ClearHover();
                return;
            }

            // If the camera is currently dragging/swiping, cancel tile click and clear hover
            if (camController != null && camController.IsDragging) {
                potentialTargetNode = null;
                isPointerPressed = false;
                ClearHover();
                return;
            }

            // Pointer Down
            if (pointerDown) {
                if (IsPointerOverUI(touchId)) {
                    isPointerPressed = false;
                    potentialTargetNode = null;
                    return;
                }

                isPointerPressed = true;
                pointerDownPos = pointerPos;
                activeTouchId = touchId;

                // Raycast to identify target node
                potentialTargetNode = RaycastNode(pointerPos);
            }

            // Pointer Held
            if (pointerHeld && isPointerPressed) {
                // If user moved more than tap threshold, treat as drag/swipe, NOT a tile click
                if (Vector2.Distance(pointerPos, pointerDownPos) > tapThresholdPixels) {
                    potentialTargetNode = null;
                }
            }

            // Pointer Up (Release)
            if (pointerUp && isPointerPressed) {
                if (!IsPointerOverUI(activeTouchId)) {
                    float travelDist = Vector2.Distance(pointerPos, pointerDownPos);
                    if (travelDist <= tapThresholdPixels && potentialTargetNode != null) {
                        potentialTargetNode.OnClicked();
                    }
                }

                isPointerPressed = false;
                potentialTargetNode = null;
                activeTouchId = -1;
            }

            // Hover tracking (when mouse moves on PC without dragging)
            if (!pointerHeld && !isPointerPressed) {
                if (IsPointerOverUI(-1)) {
                    ClearHover();
                    return;
                }

                var hovered = RaycastNode(pointerPos);
                if (hovered != currentHoveredNode) {
                    ClearHover();
                    if (hovered != null) {
                        currentHoveredNode = hovered;
                        currentHoveredNode.SetHovered(true);
                    }
                }
            }
        }

        private PathNode RaycastNode(Vector2 screenPos) {
            Ray ray = mainCam.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, tileLayerMask)) {
                return hit.collider.GetComponentInParent<PathNode>();
            }
            return null;
        }

        private bool IsPointerOverUI(int touchId) {
            if (EventSystem.current == null) return false;
            if (touchId >= 0) {
                return EventSystem.current.IsPointerOverGameObject(touchId);
            }
            return EventSystem.current.IsPointerOverGameObject();
        }

        private void ClearHover() {
            if (currentHoveredNode != null) {
                currentHoveredNode.SetHovered(false);
                currentHoveredNode = null;
            }
        }
    }
}
