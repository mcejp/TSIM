using System.Diagnostics;
using System.Numerics;

namespace TSIM.Model
{
    public struct Segment
    {
        public readonly int Id;
        public readonly SegmentType Type;
        public readonly Vector3[] ControlPoints;            // in simulation space

        public Segment(int id, SegmentType type, Vector3 start, Vector3 end)
        {
            Id = id;
            Type = type;
            ControlPoints = new[] {start, end};
        }

        public float GetLength()
        {
            Trace.Assert(ControlPoints.Length == 2);

            return (ControlPoints[1] - ControlPoints[0]).Length();
        }

        public (Vector3, Vector3) GetPointAndTangent(float t, SegmentEndpoint direction)
        {
            Trace.Assert(ControlPoints.Length == 2);

            var tangent = Vector3.Normalize(ControlPoints[1] - ControlPoints[0]);

            return (ControlPoints[0] * (1 - t) + ControlPoints[1] * t,
                    direction == SegmentEndpoint.End ? tangent : -tangent);
        }
    }
}
