using System.Numerics;

namespace TSIM.Model
{
    public class UnitClass
    {
        public readonly string Name;

        public readonly float AccelMax;

        public readonly float DecelMax;

        public readonly float VelocityMax;

        public UnitClass(string name, float accelMax, float decelMax, float velocityMax)
        {
            Name = name;
            AccelMax = accelMax;
            DecelMax = decelMax;
            VelocityMax = velocityMax;
        }
    }
}
