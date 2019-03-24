using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;
using TSIM.Model;

namespace TSIM.RailroadDatabase.Entity
{
    [Table("unit_class")]
    public class UnitClassModel
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public float Mass { get; set; }

        public float DimX { get; set; }
        public float DimY { get; set; }
        public float DimZ { get; set; }

        private UnitClassModel()
        {
        }

        public UnitClassModel(UnitClass unitClass)
        {
            Name = unitClass.Name;
            Mass = unitClass.Mass;
            (DimX, DimY, DimZ) = (unitClass.Dimensions.X, unitClass.Dimensions.Y, unitClass.Dimensions.Z);
        }

        public Model.UnitClass ToModel()
        {
            return new Model.UnitClass(Name, Mass, new Vector3(DimX, DimY, DimZ));
        }
    }
}
