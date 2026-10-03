using UnityEngine;

namespace Core.Events {
    /// <summary>
    /// Raised whenever camera rotates or completes rotation.
    /// </summary>
    public struct CameraRotationChangedEvent {
        public float CurrentYaw;
        public float TargetYaw;
        public bool IsRotating;

        public CameraRotationChangedEvent(float currentYaw, float targetYaw, bool isRotating) {
            CurrentYaw = currentYaw;
            TargetYaw = targetYaw;
            IsRotating = isRotating;
        }
    }

    /// <summary>
    /// Raised when perspective illusion alignment status changes (aligned vs disconnected).
    /// </summary>
    public struct PerspectiveAlignmentChangedEvent {
        public bool IsAligned;
        public float TargetAngle;
        public float AngleDifference;

        public PerspectiveAlignmentChangedEvent(bool isAligned, float targetAngle, float angleDifference) {
            IsAligned = isAligned;
            TargetAngle = targetAngle;
            AngleDifference = angleDifference;
        }
    }

    /// <summary>
    /// Raised when a walkable path node is clicked/tapped.
    /// </summary>
    public struct NodeClickedEvent {
        public Grid.PathNode Node;

        public NodeClickedEvent(Grid.PathNode node) {
            Node = node;
        }
    }

    /// <summary>
    /// Raised when player starts moving towards a destination.
    /// </summary>
    public struct PlayerMoveStartedEvent {
        public Grid.PathNode StartNode;
        public Grid.PathNode DestinationNode;

        public PlayerMoveStartedEvent(Grid.PathNode startNode, Grid.PathNode destinationNode) {
            StartNode = startNode;
            DestinationNode = destinationNode;
        }
    }

    /// <summary>
    /// Raised when player steps onto a specific node.
    /// </summary>
    public struct PlayerNodeReachedEvent {
        public Grid.PathNode Node;

        public PlayerNodeReachedEvent(Grid.PathNode node) {
            Node = node;
        }
    }

    /// <summary>
    /// Raised when player finishes movement at destination.
    /// </summary>
    public struct PlayerMoveCompletedEvent {
        public Grid.PathNode FinalNode;

        public PlayerMoveCompletedEvent(Grid.PathNode finalNode) {
            FinalNode = finalNode;
        }
    }

    /// <summary>
    /// Raised when the golden key is collected.
    /// </summary>
    public struct KeyCollectedEvent {
        public Vector3 Position;

        public KeyCollectedEvent(Vector3 position) {
            Position = position;
        }
    }

    /// <summary>
    /// Raised when the exit door is unlocked.
    /// </summary>
    public struct DoorUnlockedEvent {
        public Vector3 DoorPosition;

        public DoorUnlockedEvent(Vector3 doorPosition) {
            DoorPosition = doorPosition;
        }
    }

    /// <summary>
    /// Raised when player reaches open exit door and completes the level.
    /// </summary>
    public struct LevelCompletedEvent {
        public string LevelName;

        public LevelCompletedEvent(string levelName) {
            LevelName = levelName;
        }
    }

    /// <summary>
    /// Raised to request level reset.
    /// </summary>
    public struct ResetLevelEvent {
    }
}
