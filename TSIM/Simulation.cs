using System;
using System.Collections.Generic;
using System.Diagnostics;
using TSIM.Model;
using TSIM.RailroadDatabase;

namespace TSIM
{
    public class Simulation
    {
        private TimeSpan _simTimeElapsed;

        public SimulationCoordinateSpace CoordSpace { get; }
        public INetworkDatabase Network { get; }
        public IUnitDatabase Units { get; }
        public IEnumerable<IAgent> Agents => _agents;

        private readonly int?[] _currentSegmentByUnitId;
        private readonly SegmentEndpoint[] _dirByUnitId;
        private readonly float[] _tByUnitId;

        private readonly List<IAgent> _agents = new List<IAgent>();
        private readonly LoggingManager _log;

        public Simulation(SimulationCoordinateSpace coordSpace, INetworkDatabase network, IUnitDatabase units, LoggingManager log)
        {
            CoordSpace = coordSpace;
            Network = network;
            Units = units;
            _log = log;

            _currentSegmentByUnitId = new int?[units.GetNumUnits()];
            _dirByUnitId = new SegmentEndpoint[units.GetNumUnits()];
            _tByUnitId = new float[units.GetNumUnits()];

            for (var unitIndex = 0; unitIndex < Units.GetNumUnits(); unitIndex++)
            {
                var unit = Units.GetUnitByIndex(unitIndex);

                var result = Network.FindSegmentAt(unit.Pos, unit.Orientation, 0.2f, (float) (Math.PI * 0.25));

                if (result == null)
                {
                    Console.Error.WriteLine($"Simulation: could not snap unit {unitIndex} to railroad network!");
                    continue;
                }

                var (segmentId, dir, t) = result.Value;
                _currentSegmentByUnitId[unitIndex] = segmentId;
                _dirByUnitId[unitIndex] = dir;
                _tByUnitId[unitIndex] = t;
            }
        }

        public void AddAgent(IAgent agent)
        {
            _agents.Add(agent);
        }

        public (int segmentId, float t, SegmentEndpoint dir) GetUnitTrackState(int unitIndex) =>
            (_currentSegmentByUnitId[unitIndex].Value, _tByUnitId[unitIndex], _dirByUnitId[unitIndex]);

        public void Step(double dt)
        {
            _log.SetSimulatedTime(_simTimeElapsed.TotalSeconds);

            // TODO: do not use Unit.Velocity as authoritative; because we're doing on-rails simulation only
            // (at least for now), it would be more efficient to track scalar speed

            var forceByUnitIndex = new float[Units.GetNumUnits()];

            foreach (var agent in _agents)
            {
                var (unitIndex, force) = agent.Step(this, dt);

                forceByUnitIndex[unitIndex] = force;
            }

            for (var unitIndex = 0; unitIndex < Units.GetNumUnits(); unitIndex++)
            {
                if (!_currentSegmentByUnitId[unitIndex].HasValue)
                {
                    // FIXME: units not riding on a track segment
                    // - are there legitimate reasons why this would happen?
                    // - if yes, how to handle it?
                    continue;
                }

                var unit = Units.GetUnitByIndex(unitIndex);
                var speed = unit.Velocity.Length();

                // Calculate new unit speed based on force
                var newSpeed = (float)(speed + (forceByUnitIndex[unitIndex] / unit.Class.Mass) * dt);

                // Update unit position based on velocity
                // If unit is on rail, it should stay snapped
                var distanceToTravel = (speed + newSpeed) * 0.5f * dt;

                // Find out in which segment we are and how far along
                var segId = _currentSegmentByUnitId[unitIndex].Value;
                var seg = Network.GetSegmentById(segId);
                var t = _tByUnitId[unitIndex];
                var dir = _dirByUnitId[unitIndex];

                while (distanceToTravel > Single.Epsilon)
                {
                    // Find out how far further we can travel in the current segment
                    // For now, assume linear segments (no curvature)
                    // If we are travelling in the positive direction, that will be (1-t)*length
                    // If we are travelling in the negative direction, that will be t*length
                    float travellableDistance;
                    float tDir;

                    var segLength = seg.GetLength();

                    if (dir == SegmentEndpoint.End)
                    {
                        travellableDistance = (1 - t) * segLength;
                        tDir = 1;
                    }
                    else
                    {
                        travellableDistance = t * segLength;
                        tDir = -1;
                    }

                    if (travellableDistance > distanceToTravel)
                    {
                        // Only works if ds/dt is uniform for curve!!!
                        t = (float)(t + tDir * distanceToTravel / segLength);

//                        Console.WriteLine($"Unit {unitIndex} update: continue segment segId={segId} dir={dir} t={t}");
                        break;
                    }
                    else
                    {
                        distanceToTravel -= travellableDistance;

                        // We have reached the end of the current segment. Find a connecting segment
                        var candidates = Network.FindConnectingSegments(segId, dir);

                        if (candidates.Length == 0)
                        {
                            //throw new NotImplementedException("Ran out of track and cannot cope.");

                            // For now just turn around
                            dir = dir.Other();
                            continue;
                        }

                        if (candidates.Length > 1)
                        {
                            Console.WriteLine($"Unsure how to continue after segment {seg}:");
                            foreach (var link in candidates)
                            {
                                var candiSeg =
                                    Network.GetSegmentById(link.Segment1 != segId ? link.Segment1 : link.Segment2);
                                Console.WriteLine($" - candidate is link {link} segment {candiSeg}");
                            }

                            //throw new NotImplementedException("Cannot currently handle track splits");
                        }

                        // Only one candidate left. Find out more.
                        if (candidates[0].Segment1 == segId && candidates[0].Ep1 == dir)
                        {
                            segId = candidates[0].Segment2;
                            t = (candidates[0].Ep2 == SegmentEndpoint.Start ? 0 : 1);
                            dir = (candidates[0].Ep2 == SegmentEndpoint.Start ? SegmentEndpoint.End : SegmentEndpoint.Start);
                        }
                        else if (candidates[0].Segment2 == segId && candidates[0].Ep2 == dir)
                        {
                            segId = candidates[0].Segment1;
                            t = (candidates[0].Ep1 == SegmentEndpoint.Start ? 0 : 1);
                            dir = (candidates[0].Ep1 == SegmentEndpoint.Start ? SegmentEndpoint.End : SegmentEndpoint.Start);
                        }
                        else
                        {
                            Trace.Assert(false);
                        }

                        seg = Network.GetSegmentById(segId);
//                        Console.WriteLine($"Unit {unitIndex} update: new segment {seg} through link {candidates[0]} with dir={dir} t={t}");
                    }
                }

                // Update unit position, velocity & orientation based on where we end up

                var (pos, headingDir) = seg.GetPointAndTangent(t, dir);
                unit.Pos = pos;
                unit.Velocity = headingDir * newSpeed;
                unit.Orientation = Utility.DirectionVectorToQuaternion(headingDir);

                Units.UpdateUnitByIndex(unitIndex, unit);
                _currentSegmentByUnitId[unitIndex] = segId;
                _dirByUnitId[unitIndex] = dir;
                _tByUnitId[unitIndex] = t;

//                Console.WriteLine($"Unit {unitIndex} update: pos {unit.Pos} velocity {unit.Velocity}");
            }

            _simTimeElapsed += TimeSpan.FromSeconds(dt);
        }
    }
}
