using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
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

        public QuadTreeNode ToQuadTreeNode()
        {
           var node = new QuadTreeNode(new Vector3(BoundingMinX, BoundingMinY, BoundingMinZ), new Vector3(BoundingMaxX, BoundingMaxY, BoundingMaxZ));

           if (Quadrants != null && Quadrants.Count > 0)
           {
               Trace.Assert(Quadrants.Count == 4);
               Trace.Assert(Segments == null || Segments.Count == 0);
               node.Quadrants = (from quadrant in Quadrants select quadrant.ToQuadTreeNode()).ToArray();
           }

           if (Segments != null && Segments.Count > 0)
           {
               Trace.Assert(Quadrants == null || Quadrants.Count == 0);
               node.SegmentIds = (from seg in this.Segments select seg.SegmentId).ToList();
           }

           return node;
        }
    }
}
