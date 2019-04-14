using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using TSIM.Model;

namespace TSIM.RailroadDatabase.Entity
{
    [Table("station")]
    public class StationEntity
    {
        public int Id { get; set; }

        public string Name { get; set; }

        [ForeignKey("StationId")]
        public List<StationStopEntity> Stops { get; set; }

        public StationEntity()
        {
        }

        public StationEntity(Station station)
        {
            Name = station.Name;
            Stops = new List<StationStopEntity>(station.Stops.Count);

            foreach (var stop in station.Stops)
            {
                Stops.Add(new StationStopEntity(stop));
            }
        }
    }

    [Table("station_stop")]
    public class StationStopEntity
    {
        public int Id { get; set; }

        public int SegmentId { get; set; }

        public float T { get; set; }

        public StationStopEntity()
        {
        }

        public StationStopEntity(StationStop stop)
        {
            SegmentId = stop.SegmentId;
            T = stop.T;
        }
    }
}
