using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;
using TSIM.Model;

namespace TSIM.RailroadDatabase.Entity
{
    // TODO: Why exactly does this exist?
    // Is it to keep a snapshot of class definitions in save file?

    [Table("unit_class")]
    public class UnitClassModel
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public float AccelMax { get; set; }

        public float DecelMax { get; set; }

        public float VelocityMax { get; set; }

        private UnitClassModel()
        {
        }

        public UnitClassModel(UnitClass unitClass)
        {
            Name = unitClass.Name;
            AccelMax = unitClass.AccelMax;
            DecelMax = unitClass.DecelMax;
            VelocityMax = unitClass.VelocityMax;
        }

        public Model.UnitClass ToModel()
        {
            return new Model.UnitClass(Name, AccelMax, DecelMax, VelocityMax);
        }
    }
}
