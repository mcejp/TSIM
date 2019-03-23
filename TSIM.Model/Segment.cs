using System.Numerics;

namespace TSIM.Model
{
    public struct Segment
    {
        public readonly SegmentType Type;
        public readonly Vector3[] ControlPoints;            // in simulation space

        public Segment(SegmentType type, Vector3 start, Vector3 end)
        {
            this.Type = type;
            ControlPoints = new[] {start, end};
        }
    }
}
