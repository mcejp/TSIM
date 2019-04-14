using System.Collections.Generic;

namespace TSIM.Model
{
    public class Station
    {
        public string Name { get; private set; }

        public List<StationStop> Stops { get; set; }

        public Station(string name)
        {
            Name = name;
            Stops = new List<StationStop>();
        }

        public void CreateStationStop(int segmentId, float t)
        {
            Stops.Add(new StationStop(segmentId, t));
        }
    }
}
