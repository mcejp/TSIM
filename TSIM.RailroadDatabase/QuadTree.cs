using System;
using System.Collections.Generic;
using System.Numerics;
using TSIM.Model;

namespace TSIM.RailroadDatabase
{
    public class QuadTree
    {
        private readonly INetworkDatabase _network;
        public QuadTreeNode Root { get; }

        private const int MaxSegmentsInLeaf = 10;

        public QuadTree(INetworkDatabase network, Vector3 boundsMin, Vector3 boundsMax)
        {
            _network = network;
            Root = new QuadTreeNode(boundsMin, boundsMax);
        }

        public QuadTree(INetworkDatabase network, QuadTreeNode root)
        {
            _network = network;
            Root = root;
        }

        public void InsertSegment(Segment seg)
        {
            InsertSegmentUnchecked(Root, seg);
        }

        private void InsertSegment(QuadTreeNode node, Segment seg)
        {
            if (!node.IntersectedBy(seg))
            {
                throw new InvalidOperationException("Segment to add lies entirely outside node");
            }

            InsertSegmentUnchecked(node, seg);
        }

        private void InsertSegmentUnchecked(QuadTreeNode node, Segment seg)
        {
            // Is this a leaf node?
            if (node.Quadrants == null)
            {
                if (node.SegmentIds == null)
                {
                    node.SegmentIds = new List<int>(MaxSegmentsInLeaf);
                }
                else if (node.SegmentIds.Count + 1 > MaxSegmentsInLeaf)
                {
                    // Overflow? Split node and restart function.
                    SplitNode(node);
                    InsertSegmentUnchecked(node, seg);
                    return;
                }

                // Ok, insert

                node.SegmentIds.Add(seg.Id);
            }
            else
            {
                // Add segment to all children it belongs to
                foreach (var c in node.Quadrants)
                {
                    if (c.IntersectedBy(seg))
                    {
                        InsertSegmentUnchecked(c, seg);
                    }
                }
            }
        }

        private void SplitNode(QuadTreeNode node)
        {
            // Quadrants are arranged as +X+Y, -X+Y, -X-Y, +X-Y
            node.Quadrants = new QuadTreeNode[4];

            var centerX = (node.BoundingMin.X + node.BoundingMax.X) / 2;
            var centerY = (node.BoundingMin.Y + node.BoundingMax.Y) / 2;
            node.Quadrants[0] = new QuadTreeNode(new Vector3(centerX, centerY, 0),
                new Vector3(node.BoundingMax.X, node.BoundingMax.Y, 0));
            node.Quadrants[1] = new QuadTreeNode(new Vector3(node.BoundingMin.X, centerY, 0),
                new Vector3(centerX, node.BoundingMax.Y, 0));
            node.Quadrants[2] = new QuadTreeNode(new Vector3(node.BoundingMin.X, node.BoundingMin.Y, 0),
                new Vector3(centerX, centerY, 0));
            node.Quadrants[3] = new QuadTreeNode(new Vector3(centerX, node.BoundingMin.Y, 0),
                new Vector3(node.BoundingMax.X, centerY, 0));

            var segmentIds = node.SegmentIds;
            node.SegmentIds = null;

            // Simply re-insert all the segments once more after the child nodes have been created
            foreach (var segId in segmentIds)
            {
                InsertSegment(node, _network.GetSegmentById(segId));
            }
        }

        public List<(Segment, SegmentEndpoint)> FindSegmentEndpointsNear(Vector3 point, float radius)
        {
            // TODO: does this guarantee no duplicate results?

            var list = new List<(Segment, SegmentEndpoint)>();
            CollectNearbySegmentEndpoints(list, Root, point, radius);
            return list;
        }

        public HashSet<(Segment, float)> FindSegmentsNear(Vector3 point, float radius)
        {
            var collection = new HashSet<(Segment, float)>();
            CollectNearbySegments(collection, Root, point, radius);
            return collection;
        }

        private void CollectNearbySegmentEndpoints(List<(Segment, SegmentEndpoint)> list, QuadTreeNode node, Vector3 point, float radius)
        {
            // Because of the radius of tolerance, there might be results in this node even if the point lies outside
            // (but no further than the radius)
            if (Utility.DistancePointRectangle(point.X, point.Y, node.BoundingMin.X, node.BoundingMin.Y,
                    node.BoundingMax.X, node.BoundingMax.Y) > radius)
            {
                return;
            }

            if (node.Quadrants != null)
            {
                for (var i = 0; i < 4; i++)
                {
                    CollectNearbySegmentEndpoints(list, node.Quadrants[i], point, radius);
                }
            }
            else if (node.SegmentIds != null)
            {
                foreach (var segId in node.SegmentIds)
                {
                    var seg = _network.GetSegmentById(segId);

                    if ((seg.GetEndpoint(SegmentEndpoint.Start) - point).Length() < radius)
                    {
                        list.Add((seg, SegmentEndpoint.Start));
                    }

                    if ((seg.GetEndpoint(SegmentEndpoint.End) - point).Length() < radius)
                    {
                        list.Add((seg, SegmentEndpoint.End));
                    }
                }
            }
        }

        private void CollectNearbySegments(ICollection<(Segment, float)> list, QuadTreeNode node, Vector3 point, float radius)
        {
            /*
            Console.Out.WriteLine($"CollectNearbySegments -> {node.BoundingMin.X}, {node.BoundingMin.Y} .. {node.BoundingMax.X}, {node.BoundingMax.Y}");

            if (node.Quadrants == null)
            {
                if (node.SegmentIds != null)
                {
                    Console.Out.WriteLine($"    (leaf with {node.SegmentIds.Count} items)");
                }
                else
                {
                    Console.Out.WriteLine($"    (leaf empty)");
                }
            }
            */

            // Because of the radius of tolerance, there might be results in this node even if the point lies outside
            // (but no further than the radius)
            if (Utility.DistancePointRectangle(point.X, point.Y, node.BoundingMin.X, node.BoundingMin.Y,
                    node.BoundingMax.X, node.BoundingMax.Y) > radius)
            {
                return;
            }

            if (node.Quadrants != null)
            {
                for (var i = 0; i < 4; i++)
                {
                    CollectNearbySegments(list, node.Quadrants[i], point, radius);
                }
            }
            else if (node.SegmentIds != null)
            {
                foreach (var segId in node.SegmentIds)
                {
                    var seg = _network.GetSegmentById(segId);

                    var (closest, t) = seg.GetClosestPointOnSegmentToPoint(point);

                    if ((closest - point).Length() < radius)
                    {
                        list.Add((seg, t));
                    }
                }
            }
        }
    }
}
