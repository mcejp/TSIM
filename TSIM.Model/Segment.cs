using System.Diagnostics;
using System.Linq;
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

        public Segment(int id, SegmentType type, Vector3[] controlPoints)
        {
            Id = id;
            Type = type;
            ControlPoints = controlPoints;
        }

        public Vector3 GetEndpoint(SegmentEndpoint ep)
        {
            return ep switch {
                SegmentEndpoint.Start => ControlPoints[0],
                SegmentEndpoint.End => ControlPoints[ControlPoints.Length - 1]
            };
        }

        public Vector3 GetEndpointTangent(SegmentEndpoint ep, bool outwards)
        {
            Trace.Assert(ControlPoints.Length == 2);

            var tangent = Vector3.Normalize(ControlPoints[1] - ControlPoints[0]);

            if ((ep == SegmentEndpoint.Start && !outwards) || (ep == SegmentEndpoint.End && outwards))
            {
                return tangent;
            }
            else
            {
                return -tangent;
            }
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

        public bool IsEquivalent(Segment other)
        {
            Trace.Assert(ControlPoints.Length == 2);
            Trace.Assert(other.ControlPoints.Length == 2);

            return ((ControlPoints[0] - other.ControlPoints[0]).Length() < 0.001f &&
                    (ControlPoints[1] - other.ControlPoints[1]).Length() < 0.001f)
                   || ((ControlPoints[0] - other.ControlPoints[1]).Length() < 0.001f &&
                       (ControlPoints[1] - other.ControlPoints[0]).Length() < 0.001f);
        }

        public override string ToString()
        {
            string controlPoints = string.Join(",", ControlPoints.Select(x => x.ToString()).ToArray());

            return $"({nameof(Id)}: {Id}, {nameof(Type)}: {Type}, {nameof(ControlPoints)}: {controlPoints})";
        }
    }
}
