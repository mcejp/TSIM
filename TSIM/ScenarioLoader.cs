using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text.Json;
using TSIM.Model;
using TSIM.RailroadDatabase;

namespace TSIM
{
    internal class ScenarioDescriptor
    {
        public (double lat, double lon) CoordinateSystemOrigin;

        public string NetworkDatabaseFileName;
        public string UnitClassDatabaseFileName;
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
                       IUnitClassDatabase unitClassDatabase,
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
                var unitClassDatabase = new JsonUnitClassDatabase(Path.Join(basePath, desc.UnitClassDatabaseFileName));
                var units = new List<Unit>();

                foreach (var unitDesc in desc.Units)
                {
                    var rand = new Random();

                    // randomly pick a segment
                    var seg = networkDatabase.GetSegmentById(rand.Next(1, 600));

                    var class_ = unitClassDatabase.UnitClassByName(unitDesc.Class);
                    var (pos, dir) = seg.GetPointAndTangent(0.5f, SegmentEndpoint.Start);
                    var orientation = Utility.DirectionVectorToQuaternion(dir);
                    units.Add(new Unit(class_, pos, Vector3.Zero, orientation));
                }

                return (coordinateSpace, networkDatabase, unitClassDatabase, units);
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
                NetworkDatabaseFileName = document.RootElement.GetProperty("networkDatabase").GetString(),
                UnitClassDatabaseFileName = document.RootElement.GetProperty("unitClassDatabase").GetString()
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
