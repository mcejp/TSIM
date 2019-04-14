using System.Collections.Generic;
using System.Numerics;
using TSIM.Model;

namespace TSIM.RailroadDatabase
{
    public interface INetworkDatabase
    {
        IEnumerable<Segment> EnumerateSegments();
        IEnumerable<SegmentLink> EnumerateSegmentLinks();
        IEnumerable<Station> EnumerateStations();
        Segment GetSegmentById(int id);

        SegmentLink[] FindConnectingSegments(int segmentId, SegmentEndpoint ep);
        (int segmentId, SegmentEndpoint dir, float t)? FindSegmentAt(Vector3 position, Quaternion orientation,
            float radius, float maxAngle);

        /**
         * Use this only for debugging, optional visualization etc.
         */
        QuadTree? GetQuadTreeIfYouHaveOne();
    }
}
