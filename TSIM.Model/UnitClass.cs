using System.Numerics;

namespace TSIM.Model
{
    public class UnitClass
    {
        public readonly string Name;

        public readonly float Mass;

        public readonly Vector3 Dimensions;

        public UnitClass(string name, float mass, Vector3 dimensions)
        {
            Name = name;
            Mass = mass;
            Dimensions = dimensions;
        }
    }
}
