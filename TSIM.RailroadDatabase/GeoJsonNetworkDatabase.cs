using System;
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
        private readonly List<SegmentLink> _segmentLinks = new List<SegmentLink>();

        private readonly QuadTree _quadTree;

        public static GeoJsonNetworkDatabase StaticInstanceForDebug { get; private set; }

        public GeoJsonNetworkDatabase(SimulationCoordinateSpace coordinateSpace, string path)
        {
            StaticInstanceForDebug = this;

            _coordinateSpace = coordinateSpace;

            int powerOf2 = 1024;

            // Collect all segments
            using (StreamReader file = File.OpenText(path))
            {
                var document = JsonDocument.Parse(file.BaseStream);

                var maxCoordinate = ProcessFeatureCollection(document.RootElement, _segments, _segmentLinks);

                // Do you think I care??
                while (powerOf2 < maxCoordinate)
                {
                    powerOf2 *= 2;
                }
            }

            // First-pass quad tree, remove duplicates
            _quadTree = new QuadTree(this, new Vector3(-powerOf2, -powerOf2, 0), new Vector3(powerOf2, powerOf2, 0));
            List<Segment> uniqueSegments = new List<Segment>();

            foreach (var seg in _segments)
            {
                var candidates = _quadTree.FindSegmentEndpointsNear(seg.GetEndpoint(SegmentEndpoint.Start), 0.001f);

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

                _quadTree.InsertSegment(seg);
                uniqueSegments.Add(new Segment(1 + uniqueSegments.Count, seg.Type, seg.ControlPoints));
            }

            // Second-pass quad tree
            _quadTree = new QuadTree(this, new Vector3(-powerOf2, -powerOf2, 0), new Vector3(powerOf2, powerOf2, 0));
            _segments = uniqueSegments;

            foreach (var seg in _segments)
            {
                _quadTree.InsertSegment(seg);
            }

            // Discover segment links
            foreach (var seg in _segments)
            {
                float range = 0.2f;

                // Check both endpoints for possible connections
                AddLinksForSegmentEndpoint(seg, SegmentEndpoint.Start, range);
                AddLinksForSegmentEndpoint(seg, SegmentEndpoint.End, range);
            }
        }

        private void AddLinksForSegmentEndpoint(Segment seg, SegmentEndpoint ep, float range)
        {
            var point = seg.GetEndpoint(ep);

            // Get a list of (segment, endpoint) tuples around point of interest
            var candidates = FindSegmentEndpointsNear(point, range);

            foreach (var (candiSeg, candiEp) in candidates)
            {
                var linkId = 1 + _segmentLinks.Count;

                // Only add if candidate ID precedes segment ID -- this prevents adding everything twice
                // We're also not interested in cases where (seg1, ep1) == (seg2, ep2)
                if (seg.Id < candiSeg.Id)
                {
                    // TODO: we could also check 1st-order continuity (tangent)

                    _segmentLinks.Add(new SegmentLink(linkId, seg.Id, ep, candiSeg.Id, candiEp));
                }
            }
        }

        private IEnumerable<(Segment seg, SegmentEndpoint ep)> FindSegmentEndpointsNear(Vector3 point, float radius)
        {
            return _quadTree.FindSegmentEndpointsNear(point, radius);
        }

        public IEnumerable<Segment> EnumerateSegments() => _segments;
        public IEnumerable<SegmentLink> EnumerateSegmentLinks() => _segmentLinks;

        public SegmentLink[] FindConnectingSegments(int segmentId, SegmentEndpoint ep)
        {
            throw new NotImplementedException();
        }

        public Segment GetSegmentById(int id)
        {
            return _segments[id - 1];
        }

        private float ProcessFeatureCollection(in JsonElement featureCollection, List<Segment> segments, List<SegmentLink> segmentLinks)
        {
            Trace.Assert(featureCollection.GetProperty("type").GetString().Equals("FeatureCollection"));

            float maxCoordinate = 0;

            foreach (var feature in featureCollection.GetProperty("features").EnumerateArray())
            {
                maxCoordinate = Math.Max(maxCoordinate, ProcessFeature(feature, segments, segmentLinks));
            }

            return maxCoordinate;
        }

        private float ProcessFeature(in JsonElement feature, List<Segment> segments, List<SegmentLink> segmentLinks)
        {
            Trace.Assert(feature.GetProperty("type").GetString().Equals("Feature"));

            //if (feature.GetProperty("properties").GetProperty("L_VLAK").Type == JsonValueType.Null)
            if (feature.GetProperty("properties").GetProperty("L_METRO").Type == JsonValueType.Null)
            {
                return 0;
            }

            //var vlak = feature.GetProperty("properties").GetProperty("L_VLAK");
            Trace.Assert(feature.GetProperty("geometry").GetProperty("type").GetString().Equals("LineString"));

            Vector3? lastOrNull = null;

            float maxCoordinate = 0;

            foreach (var coordinate in feature.GetProperty("geometry").GetProperty("coordinates").EnumerateArray())
            {
                Trace.Assert(coordinate.GetArrayLength() == 2);
                var (lon, lat) = (coordinate[0].GetDouble(), coordinate[1].GetDouble());        // beware the order!!
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

        public QuadTree GetQuadTreeForDebug() => _quadTree;
    }
}
