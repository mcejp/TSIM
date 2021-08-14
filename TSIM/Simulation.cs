using System;
using System.Collections.Generic;
using System.Diagnostics;
using TSIM.Model;
using TSIM.RailroadDatabase;

namespace TSIM
{
    struct UnitProperties {
        public TrainControlStack Controller;
        public int AccelerationPin, VelocityPin, SegmentIdPin, TPin;
    }

    public class Simulation
    {
        public TimeSpan SimTimeElapsed; // class Serialization assigns this directly. what a mess

        public SimulationCoordinateSpace CoordSpace { get; }
        public INetworkDatabase Network { get; }
        public IUnitDatabase Units { get; }
        // public IEnumerable<IAgent> Agents => _agents;

        private readonly int?[] _currentSegmentByUnitId;
        private readonly SegmentEndpoint[] _dirByUnitId;
        private readonly float[] _tByUnitId;
        // TODO: All state should be moved out of Simulation into an explicit state container
        private readonly UnitProperties[] _unitProperties;

        // private readonly List<IAgent> _agents = new List<IAgent>();
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
            _unitProperties = new UnitProperties[units.GetNumUnits()];

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

                var eh = _log.GetEntityHandle(_unitProperties[unitIndex].GetType(), unitIndex);
                _unitProperties[unitIndex] = new UnitProperties{
                    Controller = new TrainControlStack(unitIndex, _log, Network),
                    AccelerationPin = _log.GetSignalPin(eh, "acceleration"),
                    VelocityPin = _log.GetSignalPin(eh, "velocity"),
                    SegmentIdPin = _log.GetSignalPin(eh, "segmentId"),
                    TPin = _log.GetSignalPin(eh, "t"),
                };

                _unitProperties[unitIndex].Controller.GoAutoSchedule();
            }
        }

        public IDictionary<int, TrainControlStateSummary> GetControllerStateSummary() {
            var summaryMap = new Dictionary<int, TrainControlStateSummary>();
            for (var unitIndex = 0; unitIndex < Units.GetNumUnits(); unitIndex++) {
                summaryMap.Add(unitIndex, _unitProperties[unitIndex].Controller.GetStateSummary());
            }
            return summaryMap;
        }

        public (int segmentId, float t, SegmentEndpoint dir) GetUnitTrackState(int unitIndex) =>
            (_currentSegmentByUnitId[unitIndex].Value, _tByUnitId[unitIndex], _dirByUnitId[unitIndex]);

        public void Step(double dt)
        {
            _log.SetSimulatedTime(SimTimeElapsed.TotalSeconds);
            var simTime = new DateTime(2000, 01, 01) + SimTimeElapsed;

            // TODO: do not use Unit.Velocity as authoritative; because we're doing on-rails simulation only
            // (at least for now), it would be more efficient to track scalar speed

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

                // Find out in which segment we are and how far along
                var segId = _currentSegmentByUnitId[unitIndex].Value;
                var seg = Network.GetSegmentById(segId);
                var t = _tByUnitId[unitIndex];
                var dir = _dirByUnitId[unitIndex];

                // Run train control stack
                var trainStatus = new TrainStatus{ SegmentId = segId, T = t, Dir = dir, Velocity = unit.Velocity };
                float acceleration = _unitProperties[unitIndex].Controller.Update(dt, simTime, trainStatus);

                // Console.WriteLine($"(acc = {acceleration})");

                _log.Feed(_unitProperties[unitIndex].AccelerationPin, acceleration);
                _log.Feed(_unitProperties[unitIndex].VelocityPin, speed);
                _log.Feed(_unitProperties[unitIndex].SegmentIdPin, segId);
                _log.Feed(_unitProperties[unitIndex].TPin, t);

                // It is not allowed to accelerate backwards (since we are already lumping acceleration + braking into one variable)
                double newSpeed = Math.Max(0, speed + acceleration * dt);
                double effDt;

                if (speed + acceleration * dt >= 0) {
                    effDt = dt;
                }
                else {
                    effDt = -speed / acceleration;
                    // Console.WriteLine($"effDt = {effDt}");
                    System.Diagnostics.Debug.Assert(effDt >= 0);
                }

                // Update unit position based on velocity
                // If unit is on rail, it should stay snapped
                var distanceToTravel = speed * effDt + 0.5 * acceleration * effDt * effDt;

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

                        // Track split? First try to ask the controller how to proceed.
                        if (candidates.Length > 1 && _unitProperties[unitIndex].Controller != null) {
                            (int segmentId, SegmentEndpoint entryEp)? preferredContinuation = _unitProperties[unitIndex].Controller.GetPreferredContinuationSegment(segId, dir);

                            if (preferredContinuation.HasValue) {
                                foreach (var link in candidates) {
                                    var candidateSegmentId = link.Segment1 != segId ? link.Segment1 : link.Segment2;
                                    var candidateEp = link.Segment1 != segId ? link.Ep1 : link.Ep2;

                                    if (candidateSegmentId == preferredContinuation.Value.segmentId && candidateEp == preferredContinuation.Value.entryEp) {
                                        // perfect, it's the one!
                                        candidates = new[] {link};
                                        break;
                                    }
                                }
                            }
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
                unit.Velocity = headingDir * (float)newSpeed;
                unit.Orientation = Utility.DirectionVectorToQuaternion(headingDir);

                Units.UpdateUnitByIndex(unitIndex, unit);
                _currentSegmentByUnitId[unitIndex] = segId;
                _dirByUnitId[unitIndex] = dir;
                _tByUnitId[unitIndex] = t;

//                Console.WriteLine($"Unit {unitIndex} update: pos {unit.Pos} velocity {unit.Velocity}");
            }

            SimTimeElapsed += TimeSpan.FromSeconds(dt);
        }
    }
}
