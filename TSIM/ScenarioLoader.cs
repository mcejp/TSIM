using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text.Json;
using TSIM.Model;
using TSIM.RailroadDatabase;

namespace TSIM
{
    internal class ScenarioDescriptor
    {
        public (double lat, double lon) CoordinateSystemOrigin;

        public string NetworkDatabaseFileName;
        public readonly List<UnitDescriptor> Units = new List<UnitDescriptor>();

        public class UnitDescriptor
        {
            public string Class;
        }
    }

    public static class ScenarioLoader
    {
        public static (SimulationCoordinateSpace coordinateSpace,
                       INetworkDatabase networkDatabase,
                       List<Unit> units
                       ) LoadScenario(string filename)
        {
            var basePath = Path.GetDirectoryName(filename);

            using (StreamReader file = File.OpenText(filename))
            {
                var desc = LoadScenarioDescriptor(file);

                var coordinateSpace = new SimulationCoordinateSpace(desc.CoordinateSystemOrigin.lat, desc.CoordinateSystemOrigin.lon);
                var networkDatabase = new GeoJsonNetworkDatabase(coordinateSpace,
                                                                 Path.Join(basePath, desc.NetworkDatabaseFileName));
                var units = new List<Unit>();

                foreach (var unitDesc in desc.Units)
                {
                    units.Add(new Unit(unitDesc.Class));
                }

                return (coordinateSpace, networkDatabase, units);
            }
        }

        // TODO: use JsonSerializer/Deserializer once available
        private static ScenarioDescriptor LoadScenarioDescriptor(StreamReader file)
        {
            var document = JsonDocument.Parse(file.BaseStream);

            ScenarioDescriptor desc = new ScenarioDescriptor
            {
                CoordinateSystemOrigin = (document.RootElement.GetProperty("coordinateSystemOrigin")[0].GetDouble(),
                    document.RootElement.GetProperty("coordinateSystemOrigin")[1].GetDouble()),
                NetworkDatabaseFileName = document.RootElement.GetProperty("networkDatabase").GetString()
            };

            foreach (var unitJson in document.RootElement.GetProperty("units").EnumerateArray())
            {
                var unit = new ScenarioDescriptor.UnitDescriptor();
                unit.Class = unitJson.GetProperty("class").GetString();
                desc.Units.Add(unit);
            }

            return desc;
        }
    }
}
