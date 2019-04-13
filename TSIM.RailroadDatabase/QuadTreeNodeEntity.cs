using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using TSIM.Model;

namespace TSIM.RailroadDatabase.Entity
{
    [Table("quadtree")]
    internal class QuadTreeNodeEntity
    {
        public int Id { get; set; }

        public float BoundingMinX { get; set; }
        public float BoundingMinY { get; set; }
        public float BoundingMinZ { get; set; }
        public float BoundingMaxX { get; set; }
        public float BoundingMaxY { get; set; }
        public float BoundingMaxZ { get; set; }

        [ForeignKey("ParentId")]
        public List<QuadTreeNodeEntity> Quadrants { get; set; }

        [ForeignKey("NodeId")]
        public List<QuadTreeReferencedSegment> Segments { get; set; }

        public QuadTreeNodeEntity()
        {
        }

        public QuadTreeNodeEntity(QuadTreeNode node, QuadTreeNodeEntity? parent = null)
        {
            (BoundingMinX, BoundingMinY, BoundingMinZ) = (node.BoundingMin.X, node.BoundingMin.Y, node.BoundingMin.Z);
            (BoundingMaxX, BoundingMaxY, BoundingMaxZ) = (node.BoundingMax.X, node.BoundingMax.Y, node.BoundingMax.Z);

            if (node.Quadrants != null)
            {
                Quadrants = (from quadrant in node.Quadrants select new QuadTreeNodeEntity(quadrant, this)).ToList();
            }

            if (node.SegmentIds != null)
            {
                Segments = (from segmentId in node.SegmentIds select new QuadTreeReferencedSegment(segmentId)).ToList();
            }
        }
    }
}
