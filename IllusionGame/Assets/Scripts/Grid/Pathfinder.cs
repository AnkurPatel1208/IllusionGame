using System.Collections.Generic;
using UnityEngine;

namespace Grid {
    /// <summary>
    /// Pathfinding solver for node-based isometric grid puzzles.
    /// Finds shortest valid path taking into account active perspective connections.
    /// </summary>
    public static class Pathfinder {
        public static List<PathNode> FindPath(PathNode start, PathNode target) {
            if (start == null || target == null) return null;
            if (start == target) return new List<PathNode> { start };
            if (!target.IsWalkable) return null;

            var queue = new Queue<PathNode>();
            var cameFrom = new Dictionary<PathNode, PathNode>();
            var visited = new HashSet<PathNode>();

            queue.Enqueue(start);
            visited.Add(start);

            bool found = false;

            while (queue.Count > 0) {
                var current = queue.Dequeue();

                if (current == target) {
                    found = true;
                    break;
                }

                var neighbors = current.GetActiveConnectedNeighbors();
                for (int i = 0; i < neighbors.Count; i++) {
                    var neighbor = neighbors[i];
                    if (neighbor == null || !neighbor.IsWalkable) continue;

                    if (!visited.Contains(neighbor)) {
                        visited.Add(neighbor);
                        cameFrom[neighbor] = current;
                        queue.Enqueue(neighbor);
                    }
                }
            }

            if (!found) return null;

            // Reconstruct path
            var path = new List<PathNode>();
            var curr = target;
            while (curr != null) {
                path.Add(curr);
                if (curr == start) break;
                cameFrom.TryGetValue(curr, out curr);
            }

            path.Reverse();
            return path;
        }
    }
}
