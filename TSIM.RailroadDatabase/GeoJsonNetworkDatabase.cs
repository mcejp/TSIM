using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using TSIM.Model;

namespace TSIM.RailroadDatabase
{
    public class GeoJsonNetworkDatabase : INetworkDatabase
    {
        private readonly SimulationCoordinateSpace _coordinateSpace;
        private readonly List<Segment> _segments = new List<Segment>();
        private readonly List<SegmentLink> _segmentLinks;

        private QuadTree _quadTree;
        private Dictionary<string, Station> _stations;

        public GeoJsonNetworkDatabase(SimulationCoordinateSpace coordinateSpace, string path)
        {
            _coordinateSpace = coordinateSpace;

            JsonDocument document;

            // Collect all segments
            using (StreamReader file = File.OpenText(path))
            {
                document = JsonDocument.Parse(file.BaseStream);
            }

            var maxCoordinate = ProcessTracks(document.RootElement, _segments);

            // Do you think I care??
            int powerOf2 = 1024;
            while (powerOf2 < maxCoordinate)
            {
                powerOf2 *= 2;
            }

            // First-pass: remove duplicates
            var uniqueSegments = FindUniqueSegments(powerOf2);

            // Second-pass: create segment links
            _segments = uniqueSegments;
            var quadTree = new QuadTree(this, new Vector3(-powerOf2, -powerOf2, 0), new Vector3(powerOf2, powerOf2, 0));

            int i = 1;
            foreach (var seg in _segments)
            {
                quadTree.InsertSegment(seg);
                i++;
            }

            var segmentLinks = new List<SegmentLink>();
            NetworkImporterUtility.CreateSegmentLinks(_segments, segmentLinks, quadTree,
                0.2f,
                (float) (Math.PI * 0.25f) // 45 degrees
            );

            // Only after we have the final set of segments is it possible to load stations
            var stations = new Dictionary<string, Station>();
            ProcessStations(document.RootElement, quadTree, stations);

            _quadTree = quadTree;
            _segmentLinks = segmentLinks;
            _stations = stations;
        }

        private List<Segment> FindUniqueSegments(int maxAbsCoordinate)
        {
            var quadTree = new QuadTree(this, new Vector3(-maxAbsCoordinate, -maxAbsCoordinate, 0),
                new Vector3(maxAbsCoordinate, maxAbsCoordinate, 0));
            List<Segment> uniqueSegments = new List<Segment>();

            foreach (var seg in _segments)
            {
                var candidates = quadTree.FindSegmentEndpointsNear(seg.GetEndpoint(SegmentEndpoint.Start), 0.001f);

                bool match = false;

                foreach (var (candiSeg, candiEp) in candidates)
                {
                    if (seg.IsEquivalent(candiSeg))
                    {
                        match = true;
                        break;
                    }
                }

                if (match)
                {
                    continue;
                }

                quadTree.InsertSegment(seg);
                uniqueSegments.Add(new Segment(1 + uniqueSegments.Count, seg.Type, seg.ControlPoints));
            }

            return uniqueSegments;
        }

//        private IEnumerable<(Segment seg, SegmentEndpoint ep)> FindSegmentEndpointsNear(Vector3 point, float radius)
//        {
//            return _quadTree.FindSegmentEndpointsNear(point, radius);
//        }

        public IEnumerable<Segment> EnumerateSegments() => _segments;
        public IEnumerable<SegmentLink> EnumerateSegmentLinks() => _segmentLinks;
        public IEnumerable<Station> EnumerateStations() => _stations.Values;

        public SegmentLink[] FindConnectingSegments(int segmentId, SegmentEndpoint ep)
        {
            throw new NotImplementedException();
        }

        public (Station station, StationStop stop, float distance, TrajectorySegment[] plan)? FindNearestStationAlongTrack(int segmentId, float t,
            SegmentEndpoint dir, bool verbose)
        {
            throw new NotImplementedException();
        }

        public QuadTree GetQuadTree() => _quadTree;
        public QuadTree? GetQuadTreeIfYouHaveOne() => _quadTree;

        public (int segmentId, SegmentEndpoint dir, float t)? FindSegmentAt(Vector3 position, Quaternion orientation,
            float radius, float maxAngle)
        {
            throw new NotImplementedException();
        }

        public Segment GetSegmentById(int id)
        {
            return _segments[id - 1];
        }

        private float ProcessTracks(JsonElement featureCollection, List<Segment> segments)
        {
            Trace.Assert(featureCollection.GetProperty("type").GetString().Equals("FeatureCollection"));

            float maxCoordinate = 0;

            foreach (var feature in featureCollection.GetProperty("features").EnumerateArray())
            {
                maxCoordinate = Math.Max(maxCoordinate, ProcessTrackFeature(feature, segments));
            }

            return maxCoordinate;
        }

        private void ProcessStations(JsonElement featureCollection, QuadTree quadTree, IDictionary<string, Station> stations)
        {
            Trace.Assert(featureCollection.GetProperty("type").GetString().Equals("FeatureCollection"));

            foreach (var feature in featureCollection.GetProperty("features").EnumerateArray())
            {
                ProcessStationFeature(feature, quadTree, stations);
            }
        }

        private float ProcessTrackFeature(JsonElement feature, List<Segment> segments)
        {
            Trace.Assert(feature.GetProperty("type").GetString().Equals("Feature"));

            if (feature.GetProperty("geometry").GetProperty("type").GetString().Equals("LineString"))
            {
                var coordinates = feature.GetProperty("geometry").GetProperty("coordinates").EnumerateArray();

                return ProcessLineString(segments, coordinates);
            }
            else if (feature.GetProperty("geometry").GetProperty("type").GetString().Equals("MultiLineString"))
            {
                float maxCoordinate = 0;

                foreach (JsonElement coordinates in feature.GetProperty("geometry").GetProperty("coordinates").EnumerateArray())
                {
                    maxCoordinate = Math.Max(maxCoordinate, ProcessLineString(segments, coordinates.EnumerateArray()));
                }

                return maxCoordinate;
            }

            return 0;
        }

        private float ProcessLineString(List<Segment> segments, JsonElement.ArrayEnumerator coordinates)
        {
            float maxCoordinate = 0;
            Vector3? lastOrNull = null;

            foreach (var coordinate in coordinates)
            {
                Trace.Assert(coordinate.GetArrayLength() == 2);
                var (lon, lat) = (coordinate[0].GetDouble(), coordinate[1].GetDouble()); // beware the order!!
                // also see https://macwright.org/lonlat/
                var vec = _coordinateSpace.To(lat, lon);

                maxCoordinate = Math.Max(maxCoordinate, Math.Max(Math.Abs(vec.X), Math.Abs(vec.Y)));

                if (lastOrNull is Vector3 last)
                {
                    var segmentId = 1 + segments.Count;
                    var seg = new Segment(segmentId, SegmentType.Rail, last, vec);
                    segments.Add(seg);
                }

                lastOrNull = vec;
            }

            return maxCoordinate;
        }

        private void ProcessStationFeature(JsonElement feature, QuadTree quadTree, IDictionary<string, Station> stations)
        {
            Trace.Assert(feature.GetProperty("type").GetString().Equals("Feature"));

            if (feature.GetProperty("geometry").GetProperty("type").GetString().Equals("Point"))
            {
                // Find or create Station
                var stationName = feature.GetProperty("properties").GetProperty("name").GetString();

                if (stationName == null)
                {
                    return;
                }

                var coordinate = feature.GetProperty("geometry").GetProperty("coordinates");
                Trace.Assert(coordinate.GetArrayLength() == 2);
                var (lon, lat) = (coordinate[0].GetDouble(), coordinate[1].GetDouble()); // beware the order!!
                // also see https://macwright.org/lonlat/
                var vec = _coordinateSpace.To(lat, lon);

                var radius = 20.0f;
                var candidates = quadTree.FindSegmentsNear(vec, radius);
                if (!candidates.Any())
                {
                    Console.Error.WriteLine($"Warning: couldn't snap station {stationName} to network (radius {radius:F0} meters)");
                    return;
                }

                var (seg, t) = candidates.First();

                Station st;

                if (!stations.TryGetValue(stationName, out st))
                {
                    st = new Station(stationName);
                    stations[st.Name] = st;
                }

                st.CreateStationStop(seg.Id, t);
            }
        }
    }
}
