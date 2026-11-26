using System.Collections;
using Core.Events;
using Grid;
using Player;
using UnityEngine;

namespace Gameplay {
    /// <summary>
    /// Represents the glowing level exit archway at the top of the tower.
    /// Triggers celebratory visual flair, victory fanfare, and LevelCompletedEvent
    /// when the player reaches the summit node.
    /// </summary>
    public class LevelGoal : MonoBehaviour, IEventListener<PlayerNodeReachedEvent> {
        [Header("Target Goal Node")]
        [SerializeField] private PathNode goalNode;
        [SerializeField] private string levelName = "Level 2 – The Rotary Crossing";

        [Header("Celebration FX")]
        [SerializeField] private ParticleSystem celebrationParticles;
        [SerializeField] private Light portalGlowLight;
        [SerializeField] private AudioSource victoryAudio;

        private bool hasCompleted;

        private void OnEnable() {
            EventBus<PlayerNodeReachedEvent>.Subscribe(this);
        }

        private void OnDisable() {
            EventBus<PlayerNodeReachedEvent>.Unsubscribe(this);
        }

        public void Configure(PathNode node, string name) {
            goalNode = node;
            levelName = name;
        }

        public void OnEventRaised(PlayerNodeReachedEvent eventData) {
            if (hasCompleted) return;

            if (eventData.Node == goalNode && goalNode != null) {
                hasCompleted = true;
                StartCoroutine(CelebrateAndCompleteRoutine());
            }
        }

        private IEnumerator CelebrateAndCompleteRoutine() {
            Debug.Log($"<color=yellow>[LevelGoal]</color> Player reached summit portal! Completing {levelName}!");

            if (celebrationParticles != null) {
                celebrationParticles.Play();
            }

            if (portalGlowLight != null) {
                float startIntensity = portalGlowLight.intensity;
                float elapsed = 0f;
                while (elapsed < 0.6f) {
                    elapsed += Time.deltaTime;
                    portalGlowLight.intensity = Mathf.Lerp(startIntensity, startIntensity * 2.2f, elapsed / 0.6f);
                    yield return null;
                }
            }

            if (victoryAudio != null) {
                victoryAudio.Play();
            }

            yield return new WaitForSeconds(0.8f);

            EventBus<LevelCompletedEvent>.Raise(new LevelCompletedEvent(levelName));
        }
    }
}
