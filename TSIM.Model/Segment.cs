using System;
using System.Diagnostics;
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

        public (Vector3, Quaternion) GetMidpoint()
        {
            Trace.Assert(ControlPoints.Length == 2);

            return ((ControlPoints[0] + ControlPoints[1]) * 0.5f, Utility.DirectionVectorToQuaternion(ControlPoints[1] - ControlPoints[0]));
        }
    }
}
