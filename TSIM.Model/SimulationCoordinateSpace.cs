using System;
using System.Numerics;

namespace TSIM.Model
{
    public class SimulationCoordinateSpace
    {
        public readonly double OriginLat;        // latitude north
        public readonly double OriginLon;        // longitude east

        private readonly float _cos;
        private const double R = 6.371e6;

        public SimulationCoordinateSpace(double originLat, double originLon)
        {
            OriginLat = originLat;
            OriginLon = originLon;

            _cos = (float) Math.Cos(OriginLat * (Math.PI / 180));
        }

        public Vector3 To(in double lat, in double lon)
        {
            double dlat = (lat - OriginLat) * (Math.PI / 180);
            double dlon = (lon - OriginLon) * (Math.PI / 180);

            var x = _cos * R * dlon;
            var y = R * dlat;

            return new Vector3((float) x, (float) y, 0);
        }
    }
}
