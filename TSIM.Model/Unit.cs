using System.Numerics;
using System.Text.Json.Serialization;

namespace TSIM.Model
{
    public struct Unit
    {
        public readonly UnitClass Class;
        public Vector3 Pos;
        public Vector3 Velocity;
        public Quaternion Orientation;

        [JsonConstructor]   // without this, JsonDeserialize fails to initialize @Class
        public Unit(UnitClass @class, Vector3 pos, Vector3 velocity, Quaternion orientation)
        {
            Class = @class;
            Pos = pos;
            Velocity = velocity;
            Orientation = orientation;
        }
    }
}
