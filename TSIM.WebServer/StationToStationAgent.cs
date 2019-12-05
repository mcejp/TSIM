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

        private TrajectorySegment[]? _plan;
        private Station? _planStation;

        private float? _lastDistanceToGoal;
        private float? _lastEstimatedSecondsToGoal;

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
                    _planStation = station;
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
            float? estimatedSecondsToGoal = null;

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
                    // TODO: log predicted vs arrived time

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
                // The curve is like this:
                //  - less than 1 meter away -> target speed 1 m/s
                //  - less than 100 meters away -> target speed ramp to 10 m/s
                //  - less than 740 meters away: target speed ramp to 80 km/h, and saturate there
                // Note that at the moment, we currently need to pass the goal, before route to the next stop is calculated.
                float maxSpeed = 80.0f / 3.6f;

                if (distToGoal < 1)
                {
                    targetSpeed = 1.0f;
                }
                else if (distToGoal < 100)
                {
                    targetSpeed = 1.0f + (distToGoal - 1) * (9.0f / 99.0f);
                }
                else
                {
                    targetSpeed = Math.Min(10 + (distToGoal - 100) * 0.03f, maxSpeed);
                }

                // Very rough ETA -- 30 seconds + distance over max speed
                estimatedSecondsToGoal = 30 + theDistToGoal / maxSpeed;

                break;
            }

            int maxAccelerate = 1_000_000;
            int maxBrake = 2_000_000;
            var force = (targetSpeed - unit.Velocity.Length()) * 500_000;
            force = Math.Min(Math.Max(force, -maxBrake), maxAccelerate);

            _log.FeedNullable(_distanceToTargetPin, theDistToGoal);
            _log.Feed(_forcePin, force);
            _log.Feed(_segmentIdPin, segmentId);
            _log.Feed(_velocityTargetPin, targetSpeed);
            _log.Feed(_velocityPin, unit.Velocity.Length());
            _log.Feed(_debugPin, t);

            _lastDistanceToGoal = theDistToGoal;
            _lastEstimatedSecondsToGoal = estimatedSecondsToGoal;

            return (_unitIndex, force);
        }

        public override string ToString()
        {
            var unit = _units.GetUnitByIndex(_unitIndex);

            if (_lastDistanceToGoal != null)
            {
                var eta = (_lastEstimatedSecondsToGoal != null) ? (DateTime.Now + TimeSpan.FromSeconds(_lastEstimatedSecondsToGoal.Value)) : (DateTime?)null;

                return $"StationToStationAgent#{_unitIndex}: next stop {_planStation?.Name}, {_lastDistanceToGoal:F0} meters, " +
                       $"{unit.Velocity.Length() * 3.6:F1} km/h, ETA {eta:HH:mm:ss}";
            }
            else
            {
                return $"StationToStationAgent#{_unitIndex}: no goal";
            }
        }
    }
}
