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
        // TODO: mark Required Fields once supported by the language

        // Eugh. Do this better once the language lets us.
//        [JsonPropertyName("coordinateSystemOrigin")] public (double lat, double lon) CoordinateSystemOrigin { get; set; }
        public double[] CoordinateSystemOrigin { get; set; }

        [JsonPropertyName("networkDatabase")] public string NetworkDatabaseFileName { get; set; }
        [JsonPropertyName("unitClassDatabase")] public string UnitClassDatabaseFileName { get; set; }
        public List<UnitDescriptor> Units { get; set; }

        public class UnitDescriptor
        {
            public string Class { get; set; }

            public float[] Pos { get; set; }
            public float[] Orientation { get; set; }
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
                    var class_ = unitClassDatabase.UnitClassByName(unitDesc.Class);

                    var randomly = false;        // Place units randomly

                    if (randomly)
                    {
                        var rand = new Random();

                        // randomly pick a track segment
                        var seg = networkDatabase.GetSegmentById(rand.Next(1, 600));

                        var (pos, dir) = seg.GetPointAndTangent(0.5f, SegmentEndpoint.Start);
                        var orientation = Utility.DirectionVectorToQuaternion(dir);

                        Console.Out.WriteLine(
                            $"{{\"class\": \"generic\", \"pos\": [{pos.X,7:F1}, {pos.Y,7:F1}, {pos.Z,5:F1}], " +
                            $"\"orientation\": [{orientation.X,6:F3}, {orientation.Y,6:F3}, {orientation.Z,6:F3}, {orientation.W,5:F3}]}},");
                        units.Add(new Unit(class_, pos, Vector3.Zero, orientation));
                    }
                    else
                    {
                        var (x, y, z) = (unitDesc.Pos[0],
                                         unitDesc.Pos[1],
                                         unitDesc.Pos[2]);
                        var pos = new Vector3(x, y, z);
                        var (xx, yy, zz, ww) = (unitDesc.Orientation[0],
                                                unitDesc.Orientation[1],
                                                unitDesc.Orientation[2],
                                                unitDesc.Orientation[3]);
                        var orientation = new Quaternion(xx, yy, zz, ww);
                        units.Add(new Unit(class_, pos, Vector3.Zero, orientation));
                    }
                }

                return (coordinateSpace, networkDatabase, unitClassDatabase, units);
            }
        }

        private static ScenarioDescriptor LoadScenarioDescriptor(StreamReader file)
        {
            var options = new JsonSerializerOptions();
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

            return JsonSerializer.Deserialize<ScenarioDescriptor>(file.ReadToEnd(), options);
        }
    }
}
