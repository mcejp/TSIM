using Priority_Queue;
using System;
using System.Collections.Generic;
using TSIM.Model;

namespace TSIM.RailroadDatabase {

class RoutePoint {
    public RoutePoint? Previous;
    public int ChainLength;
    public int SegmentId;
    public SegmentEndpoint EntryEp;
    public float CostToReach;
    public float segmentLength;
}

public class RoutePlan {
    public (int segmentId, SegmentEndpoint entryEp, float segmentLength, float goalT)[] route;
    public float totalCost;     // for the moment, length in meters
}

public class RoutePlanner {
    private readonly INetworkDatabase _network;

    public RoutePlanner(INetworkDatabase network) {
        _network = network;
    }

    public RoutePlan? PlanRoute(int originSegmentId, float originT, SegmentEndpoint originDirection,
                          int destinationSegmentId, float destinationT) {
        // TODO: handle degenerate case where origin == destination

        int MAX_ITERATIONS = 1_000;

        Console.WriteLine($"PlanRoute(({originSegmentId},{originT}->{originDirection}) ==> ({destinationSegmentId}, {destinationT}))");

        Segment originSegment = _network.GetSegmentById(originSegmentId);
        Segment destinationSegment = _network.GetSegmentById(destinationSegmentId);
        var destinationPoint = destinationSegment.GetPoint(destinationT);

        // Create priority queue for A* algorithm
        var queue = new SimplePriorityQueue<RoutePoint>();
        var added = new HashSet<(int, SegmentEndpoint)>();

        // insert segments following origin
        float initialCost = originSegment.DistanceToEndpoint(originT, originDirection);
        float heuristicCost = (originSegment.GetEndpoint(originDirection) - destinationPoint).Length();

        var candidates = _network.FindConnectingSegments(originSegmentId, originDirection);

        foreach (var c in candidates) {
            if (c.Segment1 != originSegmentId) {
                queue.Enqueue(new RoutePoint{Previous = null, ChainLength = 1,
                                             SegmentId = c.Segment1, EntryEp = c.Ep1, CostToReach = initialCost}, initialCost + heuristicCost);
                added.Add((c.Segment1, c.Ep1));
            }
            else {
                queue.Enqueue(new RoutePoint{Previous = null, ChainLength = 1,
                                             SegmentId = c.Segment2, EntryEp = c.Ep2, CostToReach = initialCost}, initialCost + heuristicCost);
                added.Add((c.Segment2, c.Ep2));
            }
        }

        for (int iteration = 0; queue.Count != 0; iteration++) {
            var candidate = queue.Dequeue();
            var segment = _network.GetSegmentById(candidate.SegmentId);
            candidate.segmentLength = segment.GetLength();
            //Console.WriteLine($"Try ({candidate.SegmentId}, {candidate.EntryEp})");

            if (candidate.SegmentId == destinationSegmentId) {
                // Found a path
                float totalCost = candidate.CostToReach + segment.DistanceToEndpoint(destinationT, candidate.EntryEp);

                Console.WriteLine($"PlanRoute finished after {iteration} iterations");
                Console.WriteLine($"Now displaying {candidate.ChainLength}-element path starting from (Segment={originSegmentId} Pos={originSegment.GetPoint(originT)})");
                Console.WriteLine($" - {originSegment.DistanceToEndpoint(originT, originDirection),6:F2}m in origin segment {originSegmentId} from t={originT} to {originDirection}");

                DisplayChain(candidate);

                Console.WriteLine($" - {segment.DistanceToEndpoint(destinationT, candidate.EntryEp),6:F2}m in destination segment {destinationSegmentId} from {candidate.EntryEp} to t={destinationT}");
                Console.WriteLine($"Total cost: {totalCost}");
                Console.WriteLine();

                var plan = new RoutePlan{route = new (int, SegmentEndpoint, float, float)[candidate.ChainLength], totalCost = totalCost};

                var point = candidate;

                for (int j = 0; point != null; j++) {
                    // Console.WriteLine($"Put {plan.route.Length - 1 - j} <= {point.SegmentId}");
                    plan.route[plan.route.Length - 1 - j].segmentId = point.SegmentId;
                    plan.route[plan.route.Length - 1 - j].entryEp = point.EntryEp;
                    plan.route[plan.route.Length - 1 - j].segmentLength = point.segmentLength;

                    if (j == 0) {
                        plan.route[plan.route.Length - 1 - j].goalT = destinationT;
                    }
                    else {
                        plan.route[plan.route.Length - 1 - j].goalT = -1.0f;
                    }

                    point = point.Previous;
                }

                return plan;
            }
            else if (iteration >= MAX_ITERATIONS) {
                Console.WriteLine($"Aborting search after too many iterations");
                return null;
            }

            initialCost = candidate.CostToReach + segment.GetLength();
            heuristicCost = (segment.GetEndpoint(candidate.EntryEp.Other()) - destinationPoint).Length();

            candidates = _network.FindConnectingSegments(candidate.SegmentId, candidate.EntryEp.Other());

            foreach (var c in candidates) {
                if (c.Segment1 != candidate.SegmentId) {
                    //Console.WriteLine($"Candidate ({c.Segment1}, {c.Ep1})");
                    if (!added.Contains((c.Segment1, c.Ep1))) {
                        queue.Enqueue(new RoutePoint{Previous = candidate, ChainLength = candidate.ChainLength + 1,
                                                     SegmentId = c.Segment1, EntryEp = c.Ep1, CostToReach = initialCost}, initialCost + heuristicCost);
                        added.Add((c.Segment1, c.Ep1));
                    }
                }
                else {
                    //Console.WriteLine($"Candidate ({c.Segment2}, {c.Ep2})");
                    if (!added.Contains((c.Segment2, c.Ep2))) {
                        queue.Enqueue(new RoutePoint{Previous = candidate, ChainLength = candidate.ChainLength + 1,
                                                     SegmentId = c.Segment2, EntryEp = c.Ep2, CostToReach = initialCost}, initialCost + heuristicCost);
                        added.Add((c.Segment2, c.Ep2));
                    }
                }
            }
        }

        return null;
    }

    private void DisplayChain(RoutePoint candidate) {
        if (candidate.Previous != null) {
            DisplayChain(candidate.Previous);
        }

        Console.WriteLine($" - RoutePoint(SegmentId = {candidate.SegmentId}, EntryEp = {candidate.EntryEp}, CostToReach = {candidate.CostToReach}, ChainLength = {candidate.ChainLength})");
    }
}

}
