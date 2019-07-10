using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Implementation of B* pathfinding algorithm.
/// </summary>
class BStarPathfinding : IPathfinding
{
    public override List<T> FindPath<T>(Dictionary<T, Dictionary<T, int>> edges, T originNode, T destinationNode, List<T> prevPath)
    {
        var lastCell = originNode;
        var path = new List<T>();

        if(prevPath.Count > 0) {
            lastCell = prevPath[0];
            path = prevPath;
        }

        if (!originNode.Equals(destinationNode)) {
            var neighbors = GetNeigbours(edges, lastCell);

            if(neighbors.Contains(destinationNode)) {
                path.Insert(0, destinationNode); 
            } else {
                path = new List<T>();
            }
        }

        return path;
    }
}



