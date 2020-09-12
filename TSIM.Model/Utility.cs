using System;
using System.Numerics;

namespace TSIM.Model
{
    public static class Utility
    {
        // https://stackoverflow.com/a/1171995
        public static Quaternion DirectionVectorToQuaternion(Vector3 dirVector)
        {
            var v1 = new Vector3(1, 0, 0);
            var v2 = Vector3.Normalize(dirVector);

            var dot = Vector3.Dot(v1, v2);

            if (dot >= 1.0 - Single.Epsilon)
            {
                return Quaternion.Identity;
            }
            else if (dot <= -1.0 + Single.Epsilon)
            {
                // assuming the vector doesn't point straight up!
                var right = Vector3.Cross(v2, new Vector3(0, 0, 1));
                var up = Vector3.Cross(v2, right);
                return new Quaternion(up, (float) Math.PI);
            }

            float s = (float)Math.Sqrt((1 + dot) * 2);
            float invs = 1 / s;

            Vector3 c = Vector3.Cross(v1, v2);

            var q = new Quaternion(c.X * invs, c.Y * invs, c.Z * invs, s * 0.5f);

            return Quaternion.Normalize(q);
        }

        public static Vector3 QuaternionToDirectionVector(Quaternion quat)
        {
            return Vector3.Transform(new Vector3(1, 0, 0), quat);
        }

        public static bool SegmentIntersectsLineSegment(Segment segment, float x1, float y1, float x2, float y2)
        {
	        if (segment.ControlPoints.Length != 2)
	        {
		        throw new NotImplementedException("Not implemented for higher-order splines");
	        }

	        var sx1 = segment.ControlPoints[0].X;
	        var sy1 = segment.ControlPoints[0].Y;
	        var ex1 = segment.ControlPoints[1].X;
	        var ey1 = segment.ControlPoints[1].Y;

	        var sx2 = x1;
	        var sy2 = y1;
	        var ex2 = x2;
	        var ey2 = y2;

	        // Algorithm adapted from https://www.habrador.com/tutorials/math/5-line-line-intersection/

			//To avoid floating point precision issues we can add a small value
			float epsilon = 0.00001f;
			bool shouldIncludeEndPoints = true;

			bool isIntersecting = false;

			float denominator = (ey2 - sy2) * (ex1 - sx1) - (ex2 - sx2) * (ey1 - sy1);

			//Make sure the denominator is > 0, if not the lines are parallel
			if (denominator != 0f)
			{
				float u_a = ((ex2 - sx2) * (sy1 - sy2) - (ey2 - sy2) * (sx1 - sx2)) / denominator;
				float u_b = ((ex1 - sx1) * (sy1 - sy2) - (ey1 - sy1) * (sx1 - sx2)) / denominator;

				//Are the line segments intersecting if the end points are the same
				if (shouldIncludeEndPoints)
				{
					//Is intersecting if u_a and u_b are between 0 and 1 or exactly 0 or 1
					if (u_a >= 0f + epsilon && u_a <= 1f - epsilon && u_b >= 0f + epsilon && u_b <= 1f - epsilon)
					{
						return true;
					}
				}
				else
				{
					//Is intersecting if u_a and u_b are between 0 and 1
					if (u_a > 0f + epsilon && u_a < 1f - epsilon && u_b > 0f + epsilon && u_b < 1f - epsilon)
					{
						return true;
					}
				}
			}

			return false;
        }

        // https://stackoverflow.com/a/18157551
        public static float DistancePointRectangle(float x, float y, float xmin, float ymin, float xmax, float ymax)
        {
            var dx = Math.Max(0, Math.Max(xmin - x, x - xmax));
            var dy = Math.Max(0, Math.Max(ymin - y, y - ymax));
            return (float) Math.Sqrt(dx * dx + dy * dy);
        }

        public static float DistanceToEndpoint(this Segment segment, float t, SegmentEndpoint toEp) {
            return toEp switch {
                SegmentEndpoint.Start => segment.GetLength() * t,
                SegmentEndpoint.End => segment.GetLength() * (1 - t),
            };
        }

        public static float DistanceToEndpoint(float segmentLength, float t, SegmentEndpoint toEp) {
            return toEp switch {
                SegmentEndpoint.Start => segmentLength * t,
                SegmentEndpoint.End => segmentLength * (1 - t),
            };
        }

        public static SegmentEndpoint Other(this SegmentEndpoint ep)
        {
            return ep switch {
                SegmentEndpoint.Start => SegmentEndpoint.End,
                SegmentEndpoint.End => SegmentEndpoint.Start
            };
        }
    }
}
