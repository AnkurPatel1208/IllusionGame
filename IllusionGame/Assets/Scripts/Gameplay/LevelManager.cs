using Core.Events;
using UnityEngine;

namespace Gameplay {
    /// <summary>
    /// Tracks overall level progress and coordinates win condition and resets.
    /// </summary>
    public class LevelManager : MonoBehaviour, IEventListener<KeyCollectedEvent>, IEventListener<LevelCompletedEvent> {
        [Header("Level Info")]
        [SerializeField] private string levelName = "Level 1 – The Perspective Bridge";

        [Header("Runtime State")]
        [SerializeField] private bool hasKey;
        [SerializeField] private bool isLevelCompleted;

        public bool HasKey => hasKey;
        public bool IsLevelCompleted => isLevelCompleted;
        public string LevelName => levelName;

        private void OnEnable() {
            EventBus<KeyCollectedEvent>.Subscribe(this);
            EventBus<LevelCompletedEvent>.Subscribe(this);
        }

        private void OnDisable() {
            EventBus<KeyCollectedEvent>.Unsubscribe(this);
            EventBus<LevelCompletedEvent>.Unsubscribe(this);
        }

        public void OnEventRaised(KeyCollectedEvent eventData) {
            hasKey = true;
            Debug.Log("[LevelManager] Key collected! Exit door is now unlocked.");
        }

        public void OnEventRaised(LevelCompletedEvent eventData) {
            isLevelCompleted = true;
            Debug.Log($"[LevelManager] Congratulations! {eventData.LevelName} completed!");
        }

        public void RestartLevel() {
            hasKey = false;
            isLevelCompleted = false;
            EventBus<ResetLevelEvent>.Raise(new ResetLevelEvent());
        }
    }
}
