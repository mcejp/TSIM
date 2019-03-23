using System;
using System.Numerics;

namespace TSIM.Model
{
    public class SimulationCoordinateSpace
    {
        private readonly double _lat;        // latitude north
        private readonly double _lon;        // longitude east
        private readonly float _cos;
        private const double R = 6.371e6;

        public SimulationCoordinateSpace(double lat, double lon)
        {
            _lat = lat;
            _lon = lon;
            _cos = (float) Math.Cos(_lat * (Math.PI / 180));
        }

        public Vector3 To(in double lat, in double lon)
        {
            double dlat = (lat - _lat) * (Math.PI / 180);
            double dlon = (lon - _lon) * (Math.PI / 180);
            
            var x = _cos * R * dlon;
            var y = R * dlat;
            
            return new Vector3((float) x, (float) y, 0);
        }
    }
}