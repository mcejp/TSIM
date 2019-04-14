namespace TSIM.Model
{
    public class StationStop
    {
        public int SegmentId { get; set; }

        public float T { get; set; }

        public StationStop(int segmentId, float t)
        {
            SegmentId = segmentId;
            T = t;
        }
    }
}
