using Project.Scripts.Shared.Pathfinding;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scripts.Server.Systems
{
    public static class Pathfinding
    {
        public static List<GridNode> FindPath(GridNode startNode, GridNode targetNode, GridSystem gridSystem)
        {
            if (startNode == null || targetNode == null || !targetNode.isWalkable)
                return null;

            MinHeap openSet = new MinHeap();
            Dictionary<GridNode, PathNode> openSetLookup = new Dictionary<GridNode, PathNode>();
            HashSet<GridNode> closedSet = new HashSet<GridNode>();

            PathNode startPathNode = new PathNode(startNode);
            startPathNode.gCost = 0;
            startPathNode.hCost = GetDistance(startNode, targetNode);
            openSet.Push(startPathNode);
            openSetLookup[startNode] = startPathNode;

            while (openSet.Count > 0)
            {
                PathNode currentNode = openSet.Pop();
                openSetLookup.Remove(currentNode.gridNode);
                closedSet.Add(currentNode.gridNode);

                if (currentNode.gridNode == targetNode)
                {
                    return RetracePath(startPathNode, currentNode);
                }

                foreach (GridNode neighbor in gridSystem.GetNeighbors(currentNode.gridNode))
                {
                    if (!neighbor.isWalkable || closedSet.Contains(neighbor)) continue;

                    int newMovementCostToNeighbor = currentNode.gCost + GetDistance(currentNode.gridNode, neighbor);

                    openSetLookup.TryGetValue(neighbor, out PathNode neighborNode);
                    if (neighborNode == null || newMovementCostToNeighbor < neighborNode.gCost)
                    {
                        if (neighborNode == null)
                        {
                            neighborNode = new PathNode(neighbor);
                            openSetLookup[neighbor] = neighborNode;
                        }

                        neighborNode.gCost = newMovementCostToNeighbor;
                        neighborNode.hCost = GetDistance(neighbor, targetNode);
                        neighborNode.parent = currentNode;
                        openSet.PushOrUpdate(neighborNode);
                    }
                }
            }
            return null;
        }

        private static List<GridNode> RetracePath(PathNode startNode, PathNode endNode)
        {
            List<GridNode> path = new List<GridNode>();
            PathNode currentNode = endNode;

            while (currentNode != startNode)
            {
                path.Add(currentNode.gridNode);
                currentNode = currentNode.parent;
            }
            path.Reverse();
            return path;
        }

        private static int GetDistance(GridNode nodeA, GridNode nodeB)
        {
            int dstX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
            int dstY = Mathf.Abs(nodeA.gridY - nodeB.gridY);
            return dstX + dstY;
        }

        private sealed class MinHeap
        {
            private readonly List<PathNode> _items = new List<PathNode>();
            private readonly Dictionary<PathNode, int> _indices = new Dictionary<PathNode, int>();

            public int Count => _items.Count;

            public void Push(PathNode node)
            {
                _items.Add(node);
                int index = _items.Count - 1;
                _indices[node] = index;
                SiftUp(index);
            }

            public void PushOrUpdate(PathNode node)
            {
                if (_indices.TryGetValue(node, out int index))
                {
                    SiftUp(index);
                    SiftDown(index);
                    return;
                }

                Push(node);
            }

            public PathNode Pop()
            {
                PathNode root = _items[0];
                int lastIndex = _items.Count - 1;
                Swap(0, lastIndex);
                _items.RemoveAt(lastIndex);
                _indices.Remove(root);

                if (_items.Count > 0)
                {
                    SiftDown(0);
                }

                return root;
            }

            private void SiftUp(int index)
            {
                while (index > 0)
                {
                    int parentIndex = (index - 1) / 2;
                    if (Compare(_items[index], _items[parentIndex]) >= 0)
                    {
                        break;
                    }

                    Swap(index, parentIndex);
                    index = parentIndex;
                }
            }

            private void SiftDown(int index)
            {
                int count = _items.Count;
                while (true)
                {
                    int left = index * 2 + 1;
                    if (left >= count)
                    {
                        return;
                    }

                    int right = left + 1;
                    int smallest = left;
                    if (right < count && Compare(_items[right], _items[left]) < 0)
                    {
                        smallest = right;
                    }

                    if (Compare(_items[index], _items[smallest]) <= 0)
                    {
                        return;
                    }

                    Swap(index, smallest);
                    index = smallest;
                }
            }

            private void Swap(int a, int b)
            {
                if (a == b)
                {
                    return;
                }

                PathNode temp = _items[a];
                _items[a] = _items[b];
                _items[b] = temp;
                _indices[_items[a]] = a;
                _indices[_items[b]] = b;
            }

            private static int Compare(PathNode a, PathNode b)
            {
                int fCompare = a.fCost.CompareTo(b.fCost);
                if (fCompare != 0)
                {
                    return fCompare;
                }

                return a.hCost.CompareTo(b.hCost);
            }
        }
    }
}
