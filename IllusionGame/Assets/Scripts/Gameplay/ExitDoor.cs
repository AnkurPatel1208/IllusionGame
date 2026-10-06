using System.Collections;
using System.Collections.Generic;
using Core.Events;
using Grid;
using UnityEngine;

namespace Gameplay {
    /// <summary>
    /// Exit portal/doorway. Unlocks when the key is collected.
    /// Completes the level when the player walks onto the exit platform behind the open door.
    /// </summary>
    public class ExitDoor : MonoBehaviour, IEventListener<KeyCollectedEvent>, IEventListener<PlayerNodeReachedEvent>, IEventListener<ResetLevelEvent> {
        [Header("Placement")]
        [Tooltip("The destination platform node inside the chamber behind the door")]
        [SerializeField] private PathNode doorNode;
        [Tooltip("The courtyard node directly in front of the door")]
        [SerializeField] private PathNode frontNode;
        [SerializeField] private List<PathNode> lockedPathNodes = new List<PathNode>();

        [Header("Door State")]
        [SerializeField] private bool isUnlocked;

        [Header("Visuals")]
        [SerializeField] private GameObject closedDoorVisual;
        [SerializeField] private GameObject openPortalLight;
        [SerializeField] private Light doorLight;
        [SerializeField] private Renderer doorFrameRenderer;

        [Header("Door Swing Settings")]
        [Tooltip("Rotation angle in degrees around Y. Positive 90 opens outward towards the courtyard, negative 90 opens inward.")]
        [SerializeField] private float openAngleY = 90f;

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

        public void Initialize(PathNode exitPlatformNode, PathNode entranceNode = null, List<PathNode> additionalLockedNodes = null) {
            doorNode = exitPlatformNode;
            frontNode = entranceNode;
            lockedPathNodes.Clear();
            if (additionalLockedNodes != null) {
                lockedPathNodes.AddRange(additionalLockedNodes);
            }
            if (doorNode != null && !lockedPathNodes.Contains(doorNode)) {
                lockedPathNodes.Add(doorNode);
            }
            foreach (var node in lockedPathNodes) {
                if (node != null) {
                    node.SetWalkable(false);
                }
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
            // When player steps onto the platform behind the door, level is complete!
            if (isUnlocked && doorNode != null && eventData.Node == doorNode) {
                Debug.Log("<color=green>[ExitDoor]</color> Player reached exit platform behind door! Raising LevelCompletedEvent...");
                EventBus<LevelCompletedEvent>.Raise(new LevelCompletedEvent("Level 1 - The Perspective Bridge"));
            }
        }

        public void OnEventRaised(ResetLevelEvent eventData) {
            isUnlocked = false;
            foreach (var node in lockedPathNodes) {
                if (node != null) {
                    node.SetWalkable(false);
                }
            }
            SetLockedVisuals();
        }

        private void UnlockDoor() {
            if (isUnlocked) return;
            isUnlocked = true;
            // Enable walking onto the platform behind the door!
            foreach (var node in lockedPathNodes) {
                if (node != null) {
                    node.SetWalkable(true);
                }
            }
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
            Quaternion openRot = startRot * Quaternion.Euler(0f, openAngleY, 0f);

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

        /// <summary>
        /// Clicking anywhere on the open doorway triggers the player to walk to the exit platform!
        /// </summary>
        private void OnMouseDown() {
            if (isUnlocked && doorNode != null) {
                doorNode.OnClicked();
            }
        }
    }
}
