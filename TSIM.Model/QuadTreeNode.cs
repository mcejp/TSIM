using System.Collections.Generic;
using System.Numerics;

namespace TSIM.Model
{
    public class QuadTreeNode
    {
        public Vector3 BoundingMin;
        public Vector3 BoundingMax;

        public QuadTreeNode[]? Quadrants;    // arranged as +X+Y, -X+Y, -X-Y, +X-Y
        public List<int> SegmentIds;

        public QuadTreeNode(Vector3 boundingMin, Vector3 boundingMax)
        {
            BoundingMin = boundingMin;
            BoundingMax = boundingMax;
        }
    }
}
