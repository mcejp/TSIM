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
                // assuming the vector doesn't point straignt up!
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
    }
}
