using System.ComponentModel.DataAnnotations.Schema;

namespace TSIM.RailroadDatabase.Entity
{
    [Table("segment_control_point")]
    public class SegmentControlPoint
    {
        public int Id { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        private SegmentControlPoint()
        {
        }

        public SegmentControlPoint(System.Numerics.Vector3 vec)
        {
            (X, Y, Z) = (vec.X, vec.Y, vec.Z);
        }

        public System.Numerics.Vector3 ToVector3()
        {
            return new System.Numerics.Vector3(X, Y, Z);
        }
    }
}
