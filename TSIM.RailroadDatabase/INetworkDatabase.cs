using System.Collections;
using System.Collections.Generic;
using TSIM.Model;

namespace TSIM.RailroadDatabase
{
    public interface INetworkDatabase
    {
        IEnumerable<Segment> EnumerateSegments();
        IEnumerable<SegmentLink> EnumerateSegmentLinks();
        Segment GetSegmentById(int id);
        SegmentLink[] FindConnectingSegments(int segmentId, SegmentEndpoint ep);
        QuadTree GetQuadTree();
    }
}
