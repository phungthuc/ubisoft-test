namespace Project.Scripts.Shared.Pathfinding
{
    public class PathNode
    {
        public GridNode gridNode;
        public PathNode parent;

        public int gCost;
        public int hCost;

        public int fCost => gCost + hCost;

        public PathNode(GridNode node)
        {
            gridNode = node;
        }
    }
}
