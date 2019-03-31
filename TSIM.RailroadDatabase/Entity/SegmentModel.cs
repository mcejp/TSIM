using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Linq;
using TSIM.Model;

namespace TSIM.RailroadDatabase.Entity
{
    [Table("segment")]
    public class SegmentModel
    {
        public int Id { get; set; }
        public SegmentType Type { get; set; }
        public ICollection<SegmentControlPoint> ControlPoints { get; set; }

        private SegmentModel()
        {
        }

        public SegmentModel(Model.Segment segment)
        {
            Id = segment.Id;
            Type = segment.Type;
            ControlPoints = new List<SegmentControlPoint>();

            foreach (var cp in segment.ControlPoints)
            {
                ControlPoints.Add(new Entity.SegmentControlPoint(cp));
            }
        }

        public Model.Segment ToModel()
        {
            var cp = ControlPoints.ToArray();

            Trace.Assert(cp.Length == 2);
            return new Model.Segment(Id, Type, cp[0].ToVector3(), cp[1].ToVector3());
        }
    }
}
