using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Numerics;
using TSIM.Model;

namespace TSIM.RailroadDatabase.Entity
{
    [Table("segment")]
    public class SegmentModel
    {
        public int Id { get; set; }
        public SegmentType Type { get; set; }

        [MaxLength(2*3*sizeof(float))]
        public byte[] ControlPoints { get; set; }

        private SegmentModel()
        {
        }

        public SegmentModel(Model.Segment segment)
        {
            Id = segment.Id;
            Type = segment.Type;
            ControlPoints = new byte[segment.ControlPoints.Length * 3 * sizeof(float)];

            var arr = new float[3 * segment.ControlPoints.Length];

            for (var i = 0; i < segment.ControlPoints.Length; i++)
            {
                segment.ControlPoints[i].CopyTo(arr, 3 * i);
            }

            Buffer.BlockCopy(arr, 0, ControlPoints, 0, arr.Length * sizeof(float));
        }

        public Model.Segment ToModel()
        {
            var arr = new float[ControlPoints.Length / sizeof(float)];
            Buffer.BlockCopy(ControlPoints, 0, arr, 0, arr.Length * sizeof(float));

            var cp = new Vector3[arr.Length / 3];
            for (var i = 0; i < cp.Length; i++)
            {
                cp[i] = new Vector3(arr[3 * i + 0], arr[3 * i + 1], arr[3 * i + 2]);
            }

            return new Model.Segment(Id, Type, cp);
        }
    }
}
