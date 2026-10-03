using System.Collections.Generic;
using Core.Events;
using UnityEngine;

namespace Grid {
    /// <summary>
    /// Represents a walkable point or tile in the isometric puzzle world.
    /// </summary>
    [SelectionBase]
    public class PathNode : MonoBehaviour {
        [Header("Graph Connections")]
        [SerializeField] private List<PathNode> neighbors = new();
        [SerializeField] private bool isWalkable = true;

        [Header("Visual Feedback")]
        [SerializeField] private Renderer tileRenderer;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color hoverColor = new Color(1f, 0.95f, 0.7f, 1f);
        [SerializeField] private Color targetColor = new Color(0.6f, 1f, 0.6f, 1f);

        [Header("Placement Offset")]
        [SerializeField] private Vector3 standingOffset = new Vector3(0, 0.5f, 0);

        // Dynamic perspective edges connected to this node
        private readonly List<PerspectiveEdge> perspectiveEdges = new();
        private MaterialPropertyBlock propBlock;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private bool isHovered;

        public bool IsWalkable => isWalkable;
        public Vector3 WalkPosition => transform.position + standingOffset;
        public IReadOnlyList<PathNode> Neighbors => neighbors;

        private void Awake() {
            propBlock = new MaterialPropertyBlock();
            if (tileRenderer == null) {
                tileRenderer = GetComponentInChildren<Renderer>();
            }
        }

        public void SetWalkable(bool walkable) {
            isWalkable = walkable;
        }

        public void AddNeighbor(PathNode node) {
            if (node != null && node != this && !neighbors.Contains(node)) {
                neighbors.Add(node);
            }
        }

        public void RemoveNeighbor(PathNode node) {
            neighbors.Remove(node);
        }

        public void RegisterPerspectiveEdge(PerspectiveEdge edge) {
            if (edge != null && !perspectiveEdges.Contains(edge)) {
                perspectiveEdges.Add(edge);
            }
        }

        public void UnregisterPerspectiveEdge(PerspectiveEdge edge) {
            perspectiveEdges.Remove(edge);
        }

        /// <summary>
        /// Returns all currently accessible neighbors (static + active perspective connections).
        /// </summary>
        public List<PathNode> GetActiveConnectedNeighbors() {
            var result = new List<PathNode>();
            
            // Add static neighbors
            for (int i = 0; i < neighbors.Count; i++) {
                var n = neighbors[i];
                if (n != null && n.IsWalkable) {
                    result.Add(n);
                }
            }

            // Add perspective neighbors if illusion edge is active
            for (int i = 0; i < perspectiveEdges.Count; i++) {
                var edge = perspectiveEdges[i];
                if (edge != null && edge.IsActive) {
                    var other = edge.GetOtherNode(this);
                    if (other != null && other.IsWalkable && !result.Contains(other)) {
                        result.Add(other);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Checks if connection to target node is via an active perspective edge.
        /// </summary>
        public bool HasActivePerspectiveEdgeTo(PathNode target, out PerspectiveEdge activeEdge) {
            activeEdge = null;
            for (int i = 0; i < perspectiveEdges.Count; i++) {
                var edge = perspectiveEdges[i];
                if (edge != null && edge.IsActive && edge.GetOtherNode(this) == target) {
                    activeEdge = edge;
                    return true;
                }
            }
            return false;
        }

        public void SetHovered(bool hovered) {
            if (!isWalkable) return;
            if (isHovered == hovered) return;
            isHovered = hovered;
            SetVisualColor(isHovered ? hoverColor : normalColor);
        }

        public void OnClicked() {
            if (!isWalkable) return;
            EventBus<NodeClickedEvent>.Raise(new NodeClickedEvent(this));
        }

        // Fallbacks for legacy input or event triggers
        private void OnMouseEnter() {
            SetHovered(true);
        }

        private void OnMouseExit() {
            SetHovered(false);
        }

        private void OnMouseDown() {
            OnClicked();
        }

        public void HighlightAsTarget(bool highlight) {
            SetVisualColor(highlight ? targetColor : (isHovered ? hoverColor : normalColor));
        }

        private void SetVisualColor(Color color) {
            if (tileRenderer == null) return;
            tileRenderer.GetPropertyBlock(propBlock);
            propBlock.SetColor(BaseColorId, color);
            tileRenderer.SetPropertyBlock(propBlock);
        }

        private void OnDrawGizmos() {
            Gizmos.color = isWalkable ? Color.cyan : Color.red;
            Gizmos.DrawWireSphere(WalkPosition, 0.15f);

            if (neighbors == null) return;
            Gizmos.color = Color.blue;
            for (int i = 0; i < neighbors.Count; i++) {
                if (neighbors[i] != null) {
                    Gizmos.DrawLine(WalkPosition, neighbors[i].WalkPosition);
                }
            }
        }
    }
}
