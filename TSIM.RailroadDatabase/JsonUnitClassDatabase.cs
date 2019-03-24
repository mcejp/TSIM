using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using Newtonsoft.Json;
using TSIM.Model;

namespace TSIM.RailroadDatabase
{
    public class JsonUnitClassDatabase : IUnitClassDatabase
    {
        private Dictionary<string, UnitClassModel> _map;

        public JsonUnitClassDatabase(string path)
        {
            using (StreamReader file = File.OpenText(path))
            {
                var all = JsonConvert.DeserializeObject<List<UnitClassModel>>(file.ReadToEnd());

                // Convert List<UnitClass> to name => UnitClass map
                _map = new Dictionary<string, UnitClassModel>(
                    from c in all select new KeyValuePair<string, UnitClassModel>(c.Name, c));
            }
        }

        public UnitClass UnitClassByName(string name)
        {
            return _map[name].ToModel();
        }
    }

    public class UnitClassModel
    {
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("mass")] public float Mass { get; set; }
        [JsonProperty("dimensions"), JsonConverter(typeof(Vector3Converter))] public Vector3 Dimensions { get; set; }

        public UnitClass ToModel()
        {
            return new UnitClass(Name, Mass, Dimensions);
        }
    }

    // TODO: move to like TSIM.Foundation
    public class Vector3Converter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var xyz = serializer.Deserialize<float[]>(reader);

            if (xyz.Length != 3)
            {
                throw new FormatException("Error converting value to Vector3");
            }

            return new Vector3(xyz[0], xyz[1], xyz[2]);
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Vector3);
        }
    }
}
