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

        public (Vector3 closest, float t) GetClosestPointOnSegmentToPoint(Vector3 point)
        {
            Trace.Assert(ControlPoints.Length == 2);

            // To find the closest point, intersect the line with its normal going through the point, and then clamp
            // calculated t to <0, 1>

            // Adapted from https://stackoverflow.com/a/9557244

            var A = ControlPoints[0];
            var B = ControlPoints[1];
            var P = point;

            Vector3 AP = P - A;       //Vector from A to P
            Vector3 AB = B - A;       //Vector from A to B

            float magnitudeAB = AB.LengthSquared();     //Magnitude of AB vector (it's length squared)
            float ABAPproduct = Vector3.Dot(AP, AB);    //The DOT product of a_to_p and a_to_b
            float t = ABAPproduct / magnitudeAB; //The normalized "distance" from a to your closest point

            if (t < 0)     //Check if P projection is over vectorAB
            {
                return (A, 0);
            }
            else if (t > 1)
            {
                return (B, 1);
            }
            else
            {
                return (A + AB * t, t);
            }
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

        public Vector3 GetPoint(float t)
        {
            Trace.Assert(ControlPoints.Length == 2);

            return ControlPoints[0] * (1 - t) + ControlPoints[1] * t;
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
