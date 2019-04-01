using System;
using System.Collections.Generic;
using System.Numerics;
using TSIM.Model;

namespace TSIM.RailroadDatabase
{
    public class QuadTree
    {
        private readonly INetworkDatabase _network;
        private readonly QuadTreeNode _root;

        private const int MaxSegmentsInLeaf = 10;

        public QuadTree(INetworkDatabase network, Vector3 boundsMin, Vector3 boundsMax)
        {
            _network = network;
            _root = new QuadTreeNode(boundsMin, boundsMax);
        }

        public void InsertSegment(Segment seg)
        {
            InsertSegmentUnchecked(_root, seg);
        }

        private void InsertSegment(QuadTreeNode node, Segment seg)
        {
            if (!Intersects(node, seg))
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
                    if (Intersects(c, seg))
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

        // TODO: this is not well written
        private static bool Intersects(QuadTreeNode node, Segment segment)
        {
            // Check if segment lies entirely within the node

            foreach (var cp in segment.ControlPoints)
            {
                if (cp.X >= node.BoundingMin.X && cp.Y >= node.BoundingMin.Y && cp.X < node.BoundingMax.X &&
                    cp.Y < node.BoundingMax.Y)
                {
                    return true;
                }
            }

            // Check if segment intersects any edge of the node's bounding box

            return Utility.SegmentIntersectsLineSegment(segment, node.BoundingMin.X, node.BoundingMin.Y,
                node.BoundingMax.X, node.BoundingMin.Y)
                   || Utility.SegmentIntersectsLineSegment(segment, node.BoundingMin.X, node.BoundingMax.Y,
                       node.BoundingMax.X, node.BoundingMax.Y)
                   || Utility.SegmentIntersectsLineSegment(segment, node.BoundingMin.X, node.BoundingMin.Y,
                       node.BoundingMin.X, node.BoundingMax.Y)
                   || Utility.SegmentIntersectsLineSegment(segment, node.BoundingMax.X, node.BoundingMin.Y,
                       node.BoundingMax.X, node.BoundingMax.Y);
        }

        public QuadTreeNode GetRootNodeForDebug()
        {
            return _root;
        }

        public List<(Segment, SegmentEndpoint)> FindSegmentEndpointsNear(Vector3 point, float radius)
        {
            var list = new List<(Segment, SegmentEndpoint)>();
            CollectNearbySegmentEndpoints(list, _root, point, radius);
            return list;
        }

        private void CollectNearbySegmentEndpoints(List<(Segment, SegmentEndpoint)> list, QuadTreeNode node, Vector3 point, float radius)
        {
            // Is this node even a candidate?

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
    }
}