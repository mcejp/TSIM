using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;

namespace TSIM.Model
{
    [Table("segment_link")]
    public class SegmentLink
    {
        public int Id { get; set; }

        public int Segment1 { get; set; }
        public SegmentEndpoint Ep1 { get; set; }

        public int Segment2 { get; set; }
        public SegmentEndpoint Ep2 { get; set; }

        public SegmentLink(int id, int segment1, SegmentEndpoint ep1, int segment2, SegmentEndpoint ep2)
        {
            Trace.Assert(segment1 <= segment2);
            Trace.Assert(segment1 != segment2 || ep1 != ep2);

            Id = id;
            Segment1 = segment1;
            Ep1 = ep1;
            Segment2 = segment2;
            Ep2 = ep2;
        }

        public override string ToString()
        {
            return $"({nameof(Id)}: {Id}, {Segment1}[{Ep1}] <=> {Segment2}[{Ep2}])";
        }
    }
}
