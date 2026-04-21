using UnityEngine;

namespace Project.Scripts.Shared.Pathfinding
{
    public class GridNode
    {
        public bool isWalkable;
        public Vector3 worldPosition;
        public int gridX, gridY;

        public GridNode(bool isWalkable, Vector3 worldPosition, int gridX, int gridY)
        {
            this.isWalkable = isWalkable;
            this.worldPosition = worldPosition;
            this.gridX = gridX;
            this.gridY = gridY;
        }
    }
}
