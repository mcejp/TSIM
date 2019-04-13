using System.ComponentModel.DataAnnotations.Schema;

namespace TSIM.RailroadDatabase.Entity
{
    [Table("quadtree_segment")]
    internal class QuadTreeReferencedSegment
    {
        public int Id { get; set; }
        public int SegmentId { get; set; }

        public SegmentModel Segment { get; set; }

        public QuadTreeReferencedSegment()
        {
        }

        public QuadTreeReferencedSegment(int segmentId)
        {
            SegmentId = segmentId;
        }
    }
}
