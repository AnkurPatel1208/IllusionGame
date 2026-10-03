using System.Collections;
using Core.Events;
using Grid;
using UnityEngine;

namespace Gameplay {
    /// <summary>
    /// Exit portal/doorway. Unlocks when the key is collected, completes level when entered.
    /// </summary>
    public class ExitDoor : MonoBehaviour, IEventListener<KeyCollectedEvent>, IEventListener<PlayerNodeReachedEvent>, IEventListener<ResetLevelEvent> {
        [Header("Placement")]
        [SerializeField] private PathNode doorNode;

        [Header("Door State")]
        [SerializeField] private bool isUnlocked;

        [Header("Visuals")]
        [SerializeField] private GameObject closedDoorVisual;
        [SerializeField] private GameObject openPortalLight;
        [SerializeField] private Light doorLight;
        [SerializeField] private Renderer doorFrameRenderer;

        public bool IsUnlocked => isUnlocked;
        public PathNode DoorNode => doorNode;

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
            if (closedDoorVisual != null) closedDoorVisual.SetActive(true);
            if (openPortalLight != null) openPortalLight.SetActive(false);
            if (doorLight != null) doorLight.intensity = 0f;
        }

        private IEnumerator UnlockAnimationRoutine() {
            if (closedDoorVisual != null) closedDoorVisual.SetActive(false);
            if (openPortalLight != null) openPortalLight.SetActive(true);

            if (doorLight != null) {
                float duration = 0.5f;
                float elapsed = 0f;
                float targetIntensity = 2.5f;

                while (elapsed < duration) {
                    elapsed += Time.deltaTime;
                    doorLight.intensity = Mathf.Lerp(0f, targetIntensity, elapsed / duration);
                    yield return null;
                }
                doorLight.intensity = targetIntensity;
            }
        }
    }
}
