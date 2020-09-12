using System.Collections.Generic;

namespace TSIM.Model
{
    public class Station
    {
        public int Id { get; private set; }

        public string Name { get; private set; }

        public ICollection<StationStop> Stops { get; set; }

        public Station(int id, string name, IEnumerable<StationStop>? stops = null)
        {
            Id = id;
            Name = name;
            Stops = stops != null ? new HashSet<StationStop>(stops) : new HashSet<StationStop>();
        }

        public void CreateStationStop(int segmentId, float t)
        {
            Stops.Add(new StationStop(segmentId, t));
        }
    }
}
