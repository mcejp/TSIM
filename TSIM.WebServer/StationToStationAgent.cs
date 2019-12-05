using System;
using System.Linq;
using TSIM.Model;
using TSIM.RailroadDatabase;

namespace TSIM.WebServer
{
    public class StationToStationAgent : IAgent
    {
        private readonly INetworkDatabase _network;
        private readonly IUnitDatabase _units;
        private readonly ISignalSink _log;
        private readonly int _unitIndex;
        private readonly int _logPin, _distanceToTargetPin, _forcePin, _segmentIdPin, _velocityPin, _velocityTargetPin, _debugPin;

        private TrajectorySegment[] _plan;

//        private DateTime _lastReport;

        public StationToStationAgent(INetworkDatabase network, IUnitDatabase units, ISignalSink log, int unitIndex)
        {
            _network = network;
            _units = units;
            _log = log;
            _unitIndex = unitIndex;

            var eh = _log.GetEntityHandle(typeof(StationToStationAgent), unitIndex);
            _logPin = _log.GetSignalPin(eh, "implFeed");
            _distanceToTargetPin = _log.GetSignalPin(eh, "distanceToTarget");
            _forcePin = _log.GetSignalPin(eh, "force");
            _segmentIdPin = _log.GetSignalPin(eh, "segmentId");
            _velocityPin = _log.GetSignalPin(eh, "velocity");
            _velocityTargetPin = _log.GetSignalPin(eh, "velocity(target)");
            _debugPin = _log.GetSignalPin(eh, "debug");

//            _network.FindNearestStationAlongTrack(1, 0.05f, SegmentEndpoint.Start, true);
        }

        public (int, float) Step(Simulation sim, double dt)
        {
            // What is the current objective?

            // Get current position and find nearest station
            var unit = _units.GetUnitByIndex(_unitIndex);
            var (segmentId, t, dir) = sim.GetUnitTrackState(_unitIndex);
            var seg = _network.GetSegmentById(segmentId);

            if (_plan == null)
            {
                // Plan for the current objective
                // TrajectoryPlan = list of (segments; dir; remaining distance to goal at end)

                // Follow track to nearest station
                // This is going to be super slow, cache results!
                var nearest = _network.FindNearestStationAlongTrack(segmentId, t, dir, false);

                if (nearest != null)
                {
                    var (station, stop, distance, plan) = nearest.Value;
                    _log.Feed(_logPin, $"Set goal: station {station.Name}, {distance:F0} m away");
                    _plan = plan;
                }
                else
                {
                    _log.Feed(_logPin, $"Cannot find any station; segmentId={segmentId} t={t} dir={dir}");
                    // FIXME: There is no throttle on calling FindNearestStationAlongTrack!
                }
            }

            float targetSpeed = 0;
            float? theDistToGoal = null;

            while (_plan != null)
            {
                var current = _plan[0];

                if (current.SegmentId != segmentId)
                {
                    if (_plan.Length > 1)
                    {
                        _plan = _plan.Skip(1).ToArray();        // FIXME: code smelly smell
//                        _log.Feed(_logPin, $"Plan advanced; dtt at entry, exit: {current.DistToGoalAtEntry} {current.DistToGoalAtExit}; " +
//                                           $"unit segmentId={segmentId} t={t} dir={dir}");
                    }
                    else
                    {
                        _plan = null;
//                        _log.Feed(_logPin, "Plan exhausted");
                    }

                    // Restart plan execution at new segment
                    continue;
                }

                var distAtEntry = current.DistToGoalAtEntry;
                // The following caluclation is incorrect, because it assumes that distAtEntry == distance at segment endpoint opposite of exit.
                // However, if the plan was made while we were already on this segment, distAtEntry will be the distance from our initial position
                var distToGoal = current.Dir switch {
                    SegmentEndpoint.End => distAtEntry - t * seg.GetLength(),
                    SegmentEndpoint.Start => distAtEntry - (1 - t) * seg.GetLength(),
                    };

                if (distToGoal <= 0)
                {
                    // We have reached or missed the goal

                    if (_plan.Length > 1)
                    {
                        _plan = _plan.Skip(1).ToArray();        // FIXME: code smelly smell and DRY
                    }
                    else
                    {
                        _plan = null;
                    }

                    continue;
                }

                theDistToGoal = distToGoal;

                // Control loop towards objective
                targetSpeed = Math.Min(distToGoal * 0.07f, 50 / 3.6f);

                break;
            }

            int maxAccelerate = 1_000_000;
            int maxBrake = 2_000_000;
            var force = (targetSpeed - unit.Velocity.Length()) * 200_000;
            force = Math.Min(Math.Max(force, -maxBrake), maxAccelerate);

            _log.FeedNullable(_distanceToTargetPin, theDistToGoal);
            _log.Feed(_forcePin, force);
            _log.Feed(_segmentIdPin, segmentId);
            _log.Feed(_velocityTargetPin, targetSpeed);
            _log.Feed(_velocityPin, unit.Velocity.Length());
            _log.Feed(_debugPin, t);

            return (_unitIndex, force);
        }
    }
}
