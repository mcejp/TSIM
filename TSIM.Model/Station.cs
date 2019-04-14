using System.Collections.Generic;

namespace TSIM.Model
{
    public class Station
    {
        public string Name { get; private set; }

        public ICollection<StationStop> Stops { get; set; }

        public Station(string name, IEnumerable<StationStop>? stops = null)
        {
            Name = name;
            Stops = stops != null ? new HashSet<StationStop>(stops) : new HashSet<StationStop>();
        }

        public void CreateStationStop(int segmentId, float t)
        {
            Stops.Add(new StationStop(segmentId, t));
        }
    }
}
