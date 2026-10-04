using System.Collections;
using Core.Events;
using Grid;
using UnityEngine;

namespace Gameplay {
    /// <summary>
    /// Exit portal/doorway. Unlocks when the key is collected, completes level when entered.
    /// Supports both primitive visual doors and Fantastic Dungeon Pack modular doors.
    /// </summary>
    public class ExitDoor : UnityEngine.MonoBehaviour, IEventListener<KeyCollectedEvent>, IEventListener<PlayerNodeReachedEvent>, IEventListener<ResetLevelEvent> {
        [Header("Placement")]
        [SerializeField] private PathNode doorNode;

        [Header("Door State")]
        [SerializeField] private bool isUnlocked;

        [Header("Visuals")]
        [SerializeField] private GameObject closedDoorVisual;
        [SerializeField] private GameObject openPortalLight;
        [SerializeField] private Light doorLight;
        [SerializeField] private Renderer doorFrameRenderer;

        private Quaternion originalDoorLocalRot = Quaternion.identity;
        private bool hasRecordedRot;

        public bool IsUnlocked => isUnlocked;
        public PathNode DoorNode => doorNode;

        private void Awake() {
            if (closedDoorVisual != null && !hasRecordedRot) {
                originalDoorLocalRot = closedDoorVisual.transform.localRotation;
                hasRecordedRot = true;
            }
        }

        private void OnEnable() {
            EventBus<KeyCollectedEvent>.Subscribe(this);
            EventBus<PlayerNodeReachedEvent>.Subscribe(this);
            EventBus<ResetLevelEvent>.Subscribe(this);
        }

        private void OnDisable() {
            EventBus<KeyCollectedEvent>.Unsubscribe(this);
            EventBus<PlayerNodeReachedEvent>.Unsubscribe(this);
            EventBus<ResetLevelEvent>.Unsubscribe(this);
        }

        public void Initialize(PathNode node) {
            doorNode = node;
            if (doorNode != null) {
                transform.position = doorNode.WalkPosition;
            }
            if (closedDoorVisual != null && !hasRecordedRot) {
                originalDoorLocalRot = closedDoorVisual.transform.localRotation;
                hasRecordedRot = true;
            }
            SetLockedVisuals();
        }

        public void OnEventRaised(KeyCollectedEvent eventData) {
            UnlockDoor();
        }

        public void OnEventRaised(PlayerNodeReachedEvent eventData) {
            if (isUnlocked && eventData.Node == doorNode) {
                EventBus<LevelCompletedEvent>.Raise(new LevelCompletedEvent("Level 1 - The Perspective Bridge"));
            }
        }

        public void OnEventRaised(ResetLevelEvent eventData) {
            isUnlocked = false;
            SetLockedVisuals();
        }

        private void UnlockDoor() {
            if (isUnlocked) return;
            isUnlocked = true;
            EventBus<DoorUnlockedEvent>.Raise(new DoorUnlockedEvent(transform.position));
            StartCoroutine(UnlockAnimationRoutine());
        }

        private void SetLockedVisuals() {
            if (closedDoorVisual != null) {
                closedDoorVisual.SetActive(true);
                if (hasRecordedRot) {
                    closedDoorVisual.transform.localRotation = originalDoorLocalRot;
                }
            }
            if (openPortalLight != null) openPortalLight.SetActive(false);
            if (doorLight != null) doorLight.intensity = 0f;
        }

        private IEnumerator UnlockAnimationRoutine() {
            if (openPortalLight != null) openPortalLight.SetActive(true);

            float duration = 0.8f;
            float elapsed = 0f;
            float targetIntensity = 2.5f;

            Quaternion startRot = closedDoorVisual != null ? closedDoorVisual.transform.localRotation : Quaternion.identity;
            Quaternion openRot = startRot * Quaternion.Euler(0f, -85f, 0f);

            while (elapsed < duration) {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

                if (closedDoorVisual != null) {
                    closedDoorVisual.transform.localRotation = Quaternion.Slerp(startRot, openRot, t);
                }

                if (doorLight != null) {
                    doorLight.intensity = Mathf.Lerp(0f, targetIntensity, t);
                }
                yield return null;
            }

            if (doorLight != null) {
                doorLight.intensity = targetIntensity;
            }
        }
    }
}
