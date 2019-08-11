using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using TSIM.Model;
using TSIM.RailroadDatabase;

namespace TSIM
{
    internal class ScenarioDescriptor
    {
        // Eugh
//        [JsonPropertyName("coordinateSystemOrigin")] public (double lat, double lon) CoordinateSystemOrigin { get; set; }
        [JsonPropertyName("coordinateSystemOrigin")] public double[] CoordinateSystemOrigin { get; set; }

        [JsonPropertyName("networkDatabase")] public string NetworkDatabaseFileName { get; set; }
        [JsonPropertyName("unitClassDatabase")] public string UnitClassDatabaseFileName { get; set; }
        [JsonPropertyName("units")] public List<UnitDescriptor> Units { get; set; }

        public class UnitDescriptor
        {
            [JsonPropertyName("class")] public string Class { get; set; }
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

                var coordinateSpace = new SimulationCoordinateSpace(desc.CoordinateSystemOrigin[0], desc.CoordinateSystemOrigin[1]);
                var networkDatabase = new GeoJsonNetworkDatabase(coordinateSpace,
                                                                 Path.Join(basePath, desc.NetworkDatabaseFileName));
                var unitClassDatabase = new JsonUnitClassDatabase(Path.Join(basePath, desc.UnitClassDatabaseFileName));
                var units = new List<Unit>();

                foreach (var unitDesc in desc.Units)
                {
                    var rand = new Random();

                    // FIXME: randomly pick a segment
                    var seg = networkDatabase.GetSegmentById(rand.Next(1, 600));

                    var class_ = unitClassDatabase.UnitClassByName(unitDesc.Class);
                    var (pos, dir) = seg.GetPointAndTangent(0.5f, SegmentEndpoint.Start);
                    var orientation = Utility.DirectionVectorToQuaternion(dir);
                    units.Add(new Unit(class_, pos, Vector3.Zero, orientation));
                }

                return (coordinateSpace, networkDatabase, unitClassDatabase, units);
            }
        }

        private static ScenarioDescriptor LoadScenarioDescriptor(StreamReader file)
        {
            return JsonSerializer.Deserialize<ScenarioDescriptor>(file.ReadToEnd());
        }
    }
}
