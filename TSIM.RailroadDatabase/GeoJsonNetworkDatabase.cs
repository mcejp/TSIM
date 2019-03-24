﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Text.Json;
using TSIM.Model;

namespace TSIM.RailroadDatabase
{
    public class GeoJsonNetworkDatabase : INetworkDatabase
    {
        private readonly SimulationCoordinateSpace _coordinateSpace;
        private readonly List<Segment> _segments = new List<Segment>();

        public GeoJsonNetworkDatabase(SimulationCoordinateSpace coordinateSpace, string path)
        {
            _coordinateSpace = coordinateSpace;

            using (StreamReader file = File.OpenText(path))
            {
                var document = JsonDocument.Parse(file.BaseStream);

                ProcessFeatureCollection(document.RootElement, _segments);
            }
        }

        public IEnumerable<Segment> EnumerateSegments()
        {
            return _segments;
        }

        private void ProcessFeatureCollection(in JsonElement featureCollection, List<Segment> segments)
        {
            Trace.Assert(featureCollection.GetProperty("type").GetString().Equals("FeatureCollection"));

            foreach (var feature in featureCollection.GetProperty("features").EnumerateArray())
            {
                ProcessFeature(feature, segments);
            }
        }

        private void ProcessFeature(in JsonElement feature, List<Segment> segments)
        {
            Trace.Assert(feature.GetProperty("type").GetString().Equals("Feature"));

            //if (feature.GetProperty("properties").GetProperty("L_VLAK").Type == JsonValueType.Null)
            if (feature.GetProperty("properties").GetProperty("L_METRO").Type == JsonValueType.Null)
            {
                return;
            }

            //var vlak = feature.GetProperty("properties").GetProperty("L_VLAK");
            Trace.Assert(feature.GetProperty("geometry").GetProperty("type").GetString().Equals("LineString"));

            Vector3? lastOrNull = null;

            foreach (var coordinate in feature.GetProperty("geometry").GetProperty("coordinates").EnumerateArray())
            {
                Trace.Assert(coordinate.GetArrayLength() == 2);
                var (lon, lat) = (coordinate[0].GetDouble(), coordinate[1].GetDouble());        // beware the order!!
                                                                                                // also see https://macwright.org/lonlat/
                var vec = _coordinateSpace.To(lat, lon);

                if (lastOrNull is Vector3 last)
                {
                    segments.Add(new Segment(SegmentType.Rail, last, vec));
                }

                lastOrNull = vec;
            }
        }
    }
}
