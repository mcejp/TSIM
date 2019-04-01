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

            // sx1 + (ex1 - sx1) * t1 = sx2 + (ex2 - sx2) * t2;
            // sy1 + (ey1 - sy1) * t1 = sy2 + (ey2 - sy2) * t2;

            // => t1 = (sx2 + (ex2 - sx2) * t2 - sx1) / (ex1 - sx1)
            // => t2 = (sy1 + (ey1 - sy1) * t1 - sy2) / (ey2 - sy2)
            // => t2 = (sy1 - sy2 + (ey1 - sy1) * ((sx2 + (ex2 - sx2) * t2 - sx1) / (ex1 - sx1))) / (ey2 - sy2)
            // => t2 * (ey2 - sy2) = sy1 - sy2 + (ey1 - sy1) * ((sx2 + (ex2 - sx2) * t2 - sx1) / (ex1 - sx1))
            // => t2 * (ey2 - sy2) * (ex1 - sx1) = (sy1 - sy2) * (ex1 - sx1) + (ey1 - sy1) * (sx2 + (ex2 - sx2) * t2 - sx1)
            // => t2 * (ey2 - sy2) * (ex1 - sx1) = (sy1 - sy2) * (ex1 - sx1) + (ey1 - sy1) * (sx2 - sx1) + (ey1 - sy1) * (ex2 - sx2) * t2
            // => t2 * (ey2 - sy2) * (ex1 - sx1) - t2 * (ey1 - sy1) * (ex2 - sx2) = (sy1 - sy2) * (ex1 - sx1) + (ey1 - sy1) * (sx2 - sx1)
            // => t2 = (sy1 - sy2) * (ex1 - sx1) + (ey1 - sy1) * (sx2 - sx1) / ((ey2 - sy2) * (ex1 - sx1) + (ey1 - sy1) * (ex2 - sx2))

            var sx1 = segment.ControlPoints[0].X;
            var sy1 = segment.ControlPoints[0].Y;
            var ex1 = segment.ControlPoints[1].X;
            var ey1 = segment.ControlPoints[1].Y;

            var sx2 = x1;
            var sy2 = y1;
            var ex2 = x2;
            var ey2 = y2;

            var t2 = (sy1 - sy2) * (ex1 - sx1) + (ey1 - sy1) * (sx2 - sx1) / ((ey2 - sy2) * (ex1 - sx1) + (ey1 - sy1) * (ex2 - sx2));
            var t1 = (sx2 + (ex2 - sx2) * t2 - sx1) / (ex1 - sx1);

            return t1 >= 0.0f && t1 <= 1.0f && t2 >= 0.0f && t2 <= 1.0f;
        }

        // https://stackoverflow.com/a/18157551
        public static float DistancePointRectangle(float x, float y, float xmin, float ymin, float xmax, float ymax)
        {
            var dx = Math.Max(0, Math.Max(xmin - x, x - xmax));
            var dy = Math.Max(0, Math.Max(ymin - y, y - ymax));
            return (float) Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
