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
        (Station station, StationStop stop, float distance, TrajectorySegment[] plan)? FindNearestStationAlongTrack(int segmentId, float t,
            SegmentEndpoint dir);
        (int segmentId, SegmentEndpoint dir, float t)? FindSegmentAt(Vector3 position, Quaternion orientation,
            float radius, float maxAngle);

        /**
         * Use this only for debugging, optional visualization etc.
         */
        QuadTree? GetQuadTreeIfYouHaveOne();
    }

    public class TrajectorySegment
    {
        public TrajectorySegment? Prev;

        public int SegmentId;
        public SegmentEndpoint Dir;
        public float DistToGoalAtEntry;
        public float DistToGoalAtExit;

        public TrajectorySegment(TrajectorySegment? prev, int segmentId, SegmentEndpoint dir, float distToGoalAtEntry, float distToGoalAtExit)
        {
            Prev = prev;
            SegmentId = segmentId;
            Dir = dir;
            DistToGoalAtEntry = distToGoalAtEntry;
            DistToGoalAtExit = distToGoalAtExit;
        }

        public override string ToString()
        {
            return $"({nameof(SegmentId)}: {SegmentId}, {nameof(Dir)}: {Dir}, " +
                   $"{nameof(DistToGoalAtEntry)}: {DistToGoalAtEntry}), {nameof(DistToGoalAtExit)}: {DistToGoalAtExit})";
        }
    }
}
