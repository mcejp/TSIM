using System.Numerics;

namespace TSIM.Model
{
    public struct Unit
    {
        public readonly UnitClass Class;
        public Vector3 Pos;
        public Vector3 Velocity;
        public Quaternion Orientation;

        public Unit(UnitClass class_, Vector3 pos, Vector3 velocity, Quaternion orientation)
        {
            Class = class_;
            Pos = pos;
            Velocity = velocity;
            Orientation = orientation;
        }
    }
}
