using System;
using System.Collections.Generic;
using System.Numerics;
using TSIM.Model;

namespace TSIM.RailroadDatabase
{
    public class NetworkImporterUtility
    {
        private static void CreateLinksForSegmentEndpoint(List<SegmentLink> segmentLinks, QuadTree quadTree, Segment seg, SegmentEndpoint ep, float range, float maxAngle)
        {
            var point = seg.GetEndpoint(ep);

            // Get a list of (segment, endpoint) tuples around point of interest
            var candidates = quadTree.FindSegmentEndpointsNear(point, range);

            var maxCosine = Math.Cos(maxAngle);

            foreach (var (candiSeg, candiEp) in candidates)
            {
                var linkId = 1 + segmentLinks.Count;

                // Only add if candidate ID precedes segment ID -- this prevents adding everything twice
                // We're also not interested in cases where (seg1, ep1) == (seg2, ep2)
                if (seg.Id < candiSeg.Id)
                {
                    // TODO: we could also check 1st-order continuity (tangent)

                    var tangent1 = seg.GetEndpointTangent(ep, true);
                    var tangent2 = candiSeg.GetEndpointTangent(candiEp, false);

                    if (Vector3.Dot(tangent1, tangent2) < maxCosine)
                    {
                        // Reject pair of segments on basis of too great angle
                        continue;
                    }

                    segmentLinks.Add(new SegmentLink(linkId, seg.Id, ep, candiSeg.Id, candiEp));
                }
            }
        }

        public static void CreateSegmentLinks(List<Segment> segments, List<SegmentLink> segmentLinks, QuadTree quadTree,
            float range, float maxAngle)
        {
            foreach (var seg in segments)
            {
                // Check both endpoints for possible connections
                CreateLinksForSegmentEndpoint(segmentLinks, quadTree, seg, SegmentEndpoint.Start, range, maxAngle);
                CreateLinksForSegmentEndpoint(segmentLinks, quadTree, seg, SegmentEndpoint.End, range, maxAngle);
            }
        }
    }
}
