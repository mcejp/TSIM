using System.ComponentModel.DataAnnotations.Schema;

namespace TSIM.RailroadDatabase.Entity
{
    [Table("simulation_coordinate_space")]
    public class SimulationCoordinateSpace
    {
        public int Id { get; set; }
        public double OriginLat { get; set; }
        public double OriginLon { get; set; }

        private SimulationCoordinateSpace()
        {
        }

        public SimulationCoordinateSpace(Model.SimulationCoordinateSpace coordinateSpace)
        {
            OriginLat = coordinateSpace.OriginLat;
            OriginLon = coordinateSpace.OriginLon;
        }

        public Model.SimulationCoordinateSpace ToModel()
        {
            return new Model.SimulationCoordinateSpace(OriginLat, OriginLon);
        }
    }
}
