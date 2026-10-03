using System.Collections;
using Core.Events;
using Grid;
using UnityEngine;

namespace Gameplay {
    /// <summary>
    /// Collectible golden key item resting on a pedestal.
    /// Collected when player reaches its node.
    /// </summary>
    public class KeyItem : MonoBehaviour, IEventListener<PlayerNodeReachedEvent>, IEventListener<ResetLevelEvent> {
        [Header("Placement")]
        [SerializeField] private PathNode attachedNode;

        [Header("Animation Settings")]
        [SerializeField] private float hoverAmplitude = 0.1f;
        [SerializeField] private float hoverFrequency = 2f;
        [SerializeField] private float rotationSpeed = 90f;

        [Header("Visual Elements")]
        [SerializeField] private GameObject visualModel;

        private Vector3 initialLocalPos;
        private bool isCollected;

        public bool IsCollected => isCollected;
        public PathNode AttachedNode => attachedNode;

        private void Awake() {
            if (visualModel != null) {
                initialLocalPos = visualModel.transform.localPosition;
            }
        }

        private void OnEnable() {
            EventBus<PlayerNodeReachedEvent>.Subscribe(this);
            EventBus<ResetLevelEvent>.Subscribe(this);
        }

        private void OnDisable() {
            EventBus<PlayerNodeReachedEvent>.Unsubscribe(this);
            EventBus<ResetLevelEvent>.Unsubscribe(this);
        }

        public void Initialize(PathNode node) {
            attachedNode = node;
            if (attachedNode != null) {
                transform.position = attachedNode.WalkPosition;
            }
        }

        private void Update() {
            if (isCollected || visualModel == null) return;

            // Idle floating bob and spin
            float yOffset = Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;
            visualModel.transform.localPosition = initialLocalPos + new Vector3(0, yOffset, 0);
            visualModel.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        }

        public void OnEventRaised(PlayerNodeReachedEvent eventData) {
            if (isCollected) return;

            if (eventData.Node == attachedNode) {
                Collect();
            }
        }

        public void OnEventRaised(ResetLevelEvent eventData) {
            isCollected = false;
            if (visualModel != null) {
                visualModel.SetActive(true);
                visualModel.transform.localScale = Vector3.one;
                visualModel.transform.localPosition = initialLocalPos;
            }
        }

        private void Collect() {
            isCollected = true;
            EventBus<KeyCollectedEvent>.Raise(new KeyCollectedEvent(transform.position));
            StartCoroutine(CollectAnimationRoutine());
        }

        private IEnumerator CollectAnimationRoutine() {
            if (visualModel == null) yield break;

            float duration = 0.35f;
            float elapsed = 0f;
            Vector3 startScale = visualModel.transform.localScale;
            Vector3 startPos = visualModel.transform.localPosition;

            while (elapsed < duration) {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                visualModel.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                visualModel.transform.localPosition = startPos + Vector3.up * (t * 0.5f);
                visualModel.transform.Rotate(Vector3.up, 360f * Time.deltaTime * 2f, Space.World);
                yield return null;
            }

            visualModel.SetActive(false);
        }
    }
}
