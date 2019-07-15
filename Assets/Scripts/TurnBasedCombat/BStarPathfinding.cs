using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Implementation of B* pathfinding algorithm.
/// </summary>
class BStarPathfinding : IPathfinding
{
    public override List<T> FindPath<T>(Dictionary<T, Dictionary<T, int>> edges, T originNode, T destinationNode, List<T> prevPath, int MovementPoints)
    {
        var lastCell = originNode;
        var path = new List<T>();

        if(prevPath.Count > 0) {
            lastCell = prevPath[0];
            path = prevPath;
        }

        if (!originNode.Equals(destinationNode)) {
            var neighbors = GetNeigbours(edges, lastCell);


            if(prevPath.Contains(destinationNode)) {
                var index = prevPath.IndexOf(destinationNode);
                path.RemoveRange(0, index);
            } else if(neighbors.Contains(destinationNode) && path.Count < MovementPoints) {
                path.Insert(0, destinationNode); 
            } else if (neighbors.Contains(destinationNode)) {
                // Do nothing, max path length reached
            } else {
                path = new List<T>();
            }
        }

        return path;
    }
}



