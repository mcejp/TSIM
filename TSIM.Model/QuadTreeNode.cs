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

        // TODO: this is not well written
        public bool IntersectedBy(Segment segment)
        {
            // Check if segment lies entirely within the node

            foreach (var cp in segment.ControlPoints)
            {
                if (cp.X >= BoundingMin.X && cp.Y >= BoundingMin.Y && cp.X < BoundingMax.X &&
                    cp.Y < BoundingMax.Y)
                {
                    return true;
                }
            }

            // Check if segment intersects any edge of the node's bounding box

            return Utility.SegmentIntersectsLineSegment(segment, BoundingMin.X, BoundingMin.Y,
                       BoundingMax.X, BoundingMin.Y)
                   || Utility.SegmentIntersectsLineSegment(segment, BoundingMin.X, BoundingMax.Y,
                       BoundingMax.X, BoundingMax.Y)
                   || Utility.SegmentIntersectsLineSegment(segment, BoundingMin.X, BoundingMin.Y,
                       BoundingMin.X, BoundingMax.Y)
                   || Utility.SegmentIntersectsLineSegment(segment, BoundingMax.X, BoundingMin.Y,
                       BoundingMax.X, BoundingMax.Y);
        }
    }
}
