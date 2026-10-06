using Core.Events;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace CameraControl {
    /// <summary>
    /// Controls an orthographic isometric camera rotating around a central puzzle pivot.
    /// Supports:
    /// - Mouse drag on PC (left-click drag or right-click drag)
    /// - Touch swipe / flick on Mobile
    /// - Keyboard rotation (Q/E or arrows)
    /// - Dynamic portrait mode adaptation (scales orthographicSize to maintain framing across all aspect ratios)
    /// - Seamless snapping to nearest 90-degree isometric angles
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class IsometricCameraController : MonoBehaviour {
        [Header("Pivot & Framing")]
        [SerializeField] private Transform pivotTarget;
        [SerializeField] private Vector3 pivotOffset = new Vector3(0, 1.5f, 0);
        [SerializeField] private float orthographicSize = 9.5f;
        [SerializeField] private float pitchAngle = 30f;
        [SerializeField] private float distance = 25f;

        [Header("Portrait / Aspect Ratio Adaptation")]
        [SerializeField] private bool adaptToPortrait = true;
        [Tooltip("Target horizontal width visible in world units to maintain in portrait mode")]
        [SerializeField] private float targetVisibleWidth = 13.5f;
        [SerializeField] private float minOrthoSize = 9.5f;

        [Header("Rotation Settings")]
        [Tooltip("Available snap angles in degrees yaw")]
        [SerializeField] private float[] snapAngles = new float[] { 45f, 135f, 225f, 315f };
        [SerializeField] private int currentSnapIndex = 1; // Default to 135 deg (separated angle)
        [SerializeField] private float rotationSpeed = 8f; // Smooth damp speed
        [SerializeField] private float dragSensitivity = 0.35f;

        [Header("Drag & Swipe Settings")]
        [SerializeField] private bool allowDrag = true;
        [SerializeField] private bool snapOnRelease = true;
        [SerializeField] private float dragStartThreshold = 10f; // Pixels of movement before drag engages
        [SerializeField] private float flickVelocityThreshold = 350f; // Pixels/second for swipe flick gesture

        private Camera cam;
        private float currentYaw;
        private float targetYaw;
        private float yawVelocity;
        private bool isDragging;
        private bool isRotating;

        // Pointer tracking for drag/swipe
        private bool isPointerDown;
        private Vector2 pointerDownPosition;
        private Vector2 lastPointerPosition;
        private float smoothedVelocityX;
        private int activeTouchId = -1;

        public float CurrentYaw => currentYaw;
        public float TargetYaw => targetYaw;
        public bool IsRotating => isRotating;
        public bool IsDragging => isDragging;
        public Camera Camera => cam;

        private void Awake() {
            cam = GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = orthographicSize;

            if (snapAngles != null && snapAngles.Length > 0) {
                currentSnapIndex = Mathf.Clamp(currentSnapIndex, 0, snapAngles.Length - 1);
                currentYaw = snapAngles[currentSnapIndex];
                targetYaw = currentYaw;
            } else {
                currentYaw = 135f;
                targetYaw = 135f;
            }
        }

        private void Start() {
            UpdateCameraTransform();
            EventBus<CameraRotationChangedEvent>.Raise(new CameraRotationChangedEvent(currentYaw, targetYaw, false));
        }

        private void Update() {
            HandleKeyboardInput();
            if (allowDrag) {
                HandlePointerDragAndSwipe();
            }
            ApplyRotation();
        }

        private void HandleKeyboardInput() {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.qKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame) {
                RotateToPreviousSnap();
            } else if (keyboard.eKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame) {
                RotateToNextSnap();
            }
        }

        private void HandlePointerDragAndSwipe() {
            Vector2 currentPointerPos = Vector2.zero;
            bool pointerDownThisFrame = false;
            bool pointerHeld = false;
            bool pointerUpThisFrame = false;
            int touchId = -1;

            // 1. Check Touch Input (Mobile)
            var touch = Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.isPressed) {
                touchId = touch.primaryTouch.touchId.ReadValue();
                currentPointerPos = touch.primaryTouch.position.ReadValue();
                pointerDownThisFrame = touch.primaryTouch.press.wasPressedThisFrame;
                pointerHeld = true;
            } else if (touch != null && touch.primaryTouch.press.wasReleasedThisFrame) {
                touchId = touch.primaryTouch.touchId.ReadValue();
                currentPointerPos = touch.primaryTouch.position.ReadValue();
                pointerUpThisFrame = true;
            }
            // 2. Check Mouse Input (PC)
            else {
                var mouse = Mouse.current;
                if (mouse != null) {
                    bool leftDown = mouse.leftButton.isPressed;
                    bool rightDown = mouse.rightButton.isPressed;
                    bool middleDown = mouse.middleButton.isPressed;

                    currentPointerPos = mouse.position.ReadValue();
                    pointerDownThisFrame = mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame || mouse.middleButton.wasPressedThisFrame;
                    pointerHeld = leftDown || rightDown || middleDown;
                    pointerUpThisFrame = mouse.leftButton.wasReleasedThisFrame || mouse.rightButton.wasReleasedThisFrame || mouse.middleButton.wasReleasedThisFrame;
                }
            }

            // Handle Pointer Down
            if (pointerDownThisFrame) {
                // Ignore if clicked on UI
                if (IsPointerOverUI(touchId)) {
                    isPointerDown = false;
                    isDragging = false;
                    return;
                }

                isPointerDown = true;
                isDragging = false;
                activeTouchId = touchId;
                pointerDownPosition = currentPointerPos;
                lastPointerPosition = currentPointerPos;
                smoothedVelocityX = 0f;
            }

            // Handle Pointer Held (Dragging / Swiping)
            if (pointerHeld && isPointerDown) {
                float totalMoveDist = Vector2.Distance(currentPointerPos, pointerDownPosition);

                if (!isDragging && totalMoveDist >= dragStartThreshold) {
                    isDragging = true;
                    lastPointerPosition = currentPointerPos;
                }

                if (isDragging) {
                    float deltaX = currentPointerPos.x - lastPointerPosition.x;
                    float instantVel = Time.deltaTime > 0f ? deltaX / Time.deltaTime : 0f;
                    smoothedVelocityX = Mathf.Lerp(smoothedVelocityX, instantVel, Time.deltaTime * 20f);

                    targetYaw += deltaX * dragSensitivity;
                    lastPointerPosition = currentPointerPos;
                }
            }

            // Handle Pointer Up (Release)
            if (pointerUpThisFrame && isPointerDown) {
                if (isDragging) {
                    isDragging = false;

                    if (snapOnRelease) {
                        // Check for swipe flick gesture
                        if (Mathf.Abs(smoothedVelocityX) > flickVelocityThreshold) {
                            if (smoothedVelocityX > 0f) {
                                RotateToNextSnap();
                            } else {
                                RotateToPreviousSnap();
                            }
                        } else {
                            SnapToNearestAngle();
                        }
                    }
                }

                isPointerDown = false;
                activeTouchId = -1;
            }
        }

        private bool IsPointerOverUI(int touchId) {
            if (EventSystem.current == null) return false;
            if (touchId >= 0) {
                return EventSystem.current.IsPointerOverGameObject(touchId);
            }
            return EventSystem.current.IsPointerOverGameObject();
        }

        public void RotateToNextSnap() {
            if (snapAngles == null || snapAngles.Length == 0) return;
            currentSnapIndex = (currentSnapIndex + 1) % snapAngles.Length;
            SetTargetYaw(snapAngles[currentSnapIndex]);
        }

        public void RotateToPreviousSnap() {
            if (snapAngles == null || snapAngles.Length == 0) return;
            currentSnapIndex--;
            if (currentSnapIndex < 0) currentSnapIndex = snapAngles.Length - 1;
            SetTargetYaw(snapAngles[currentSnapIndex]);
        }

        public void SetTargetYaw(float yaw) {
            targetYaw = NormalizeAngle(yaw);
        }

        private void SnapToNearestAngle() {
            if (snapAngles == null || snapAngles.Length == 0) return;

            float minDiff = float.MaxValue;
            int bestIndex = 0;
            float normTarget = NormalizeAngle(targetYaw);

            for (int i = 0; i < snapAngles.Length; i++) {
                float diff = Mathf.Abs(Mathf.DeltaAngle(normTarget, snapAngles[i]));
                if (diff < minDiff) {
                    minDiff = diff;
                    bestIndex = i;
                }
            }

            currentSnapIndex = bestIndex;
            targetYaw = snapAngles[bestIndex];
        }

        private void ApplyRotation() {
            float prevYaw = currentYaw;
            currentYaw = Mathf.SmoothDampAngle(currentYaw, targetYaw, ref yawVelocity, 1f / rotationSpeed);

            bool wasRotating = isRotating;
            isRotating = Mathf.Abs(Mathf.DeltaAngle(currentYaw, targetYaw)) > 0.05f || isDragging;

            if (Mathf.Abs(Mathf.DeltaAngle(prevYaw, currentYaw)) > 0.01f || wasRotating != isRotating) {
                EventBus<CameraRotationChangedEvent>.Raise(new CameraRotationChangedEvent(currentYaw, targetYaw, isRotating));
            }

            UpdateCameraTransform();
        }

        public void UpdateCameraTransform() {
            Vector3 center = (pivotTarget != null) ? pivotTarget.position + pivotOffset : pivotOffset;

            Quaternion rotation = Quaternion.Euler(pitchAngle, currentYaw, 0f);
            Vector3 direction = rotation * Vector3.back;
            transform.position = center + direction * distance;
            transform.rotation = rotation;

            if (cam == null) cam = GetComponent<Camera>();
            if (cam != null) {
                cam.orthographic = true;
                if (adaptToPortrait && cam.aspect > 0.01f) {
                    // In orthographic projection: visibleWidth = 2 * orthoSize * aspect
                    // Therefore: orthoSize = visibleWidth / (2 * aspect)
                    float portraitOrtho = (targetVisibleWidth * 0.5f) / cam.aspect;
                    cam.orthographicSize = Mathf.Max(minOrthoSize, portraitOrtho);
                } else {
                    cam.orthographicSize = orthographicSize;
                }
            }
        }

        public void SetPortraitSettings(bool adapt, float visibleWidth, float minSize) {
            adaptToPortrait = adapt;
            targetVisibleWidth = visibleWidth;
            minOrthoSize = minSize;
            UpdateCameraTransform();
        }

        public void SetPivotTarget(Transform target, Vector3 offset) {
            pivotTarget = target;
            pivotOffset = offset;
            UpdateCameraTransform();
        }

        private static float NormalizeAngle(float angle) {
            angle %= 360f;
            if (angle < 0) angle += 360f;
            return angle;
        }
    }
}
