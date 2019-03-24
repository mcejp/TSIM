using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;
using TSIM.Model;

namespace TSIM.RailroadDatabase.Entity
{
    [Table("unit")]
    public class UnitModel
    {
        public int Id { get; set; }

        public UnitClassModel Class { get; set; }

        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public float OX { get; set; }
        public float OY { get; set; }
        public float OZ { get; set; }
        public float OW { get; set; }

        private UnitModel()
        {
        }

        public UnitModel(Unit unit)
        {
            Class = new UnitClassModel(unit.Class);
            (X, Y, Z) = (unit.Pos.X, unit.Pos.Y, unit.Pos.Z);
            (OX, OY, OZ, OW) = (unit.Orientation.X, unit.Orientation.Y, unit.Orientation.Z, unit.Orientation.W);
        }

        public Unit ToModel()
        {
            return new Unit(Class.ToModel(), new Vector3(X, Y, Z), new Quaternion(OX, OY, OZ, OW));
        }
    }
}
