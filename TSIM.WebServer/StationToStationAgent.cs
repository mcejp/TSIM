using System;
using System.Linq;
using TSIM.Model;
using TSIM.RailroadDatabase;

namespace TSIM.WebServer
{
    public class StationToStationAgent : IAgent
    {
        private enum State { IDLE, BOARDING, EN_ROUTE, APPROACHING }

        private readonly INetworkDatabase _network;
        private readonly IUnitDatabase _units;
        private readonly ISignalSink _log;
        private readonly int _unitIndex;
        private readonly int _logPin, _distanceToTargetPin, _accelerationForcePin, _brakingForcePin, _segmentIdPin, _velocityPin, _velocityTargetPin, _debugPin;

        private State _state = State.IDLE;
        private TrajectorySegment[]? _plan;
        private Station? _planStation;

        private float? _lastDistanceToGoal;
        private double _boardingTimer;
        private DateTime _eta;

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
            _accelerationForcePin = _log.GetSignalPin(eh, "accelerationForce");
            _brakingForcePin = _log.GetSignalPin(eh, "brakingForce");
            _segmentIdPin = _log.GetSignalPin(eh, "segmentId");
            _velocityPin = _log.GetSignalPin(eh, "velocity");
            _velocityTargetPin = _log.GetSignalPin(eh, "velocity(target)");
            _debugPin = _log.GetSignalPin(eh, "debug");

//            _network.FindNearestStationAlongTrack(1, 0.05f, SegmentEndpoint.Start, true);
        }

        public (int, float, float) Step(Simulation sim, double dt)
        {
            // What is the current objective?
            var unit = _units.GetUnitByIndex(_unitIndex);
            var velocity = unit.Velocity.Length();

            var (segmentId, t, dir) = sim.GetUnitTrackState(_unitIndex);

            float commandedVelocity = 0;
            float maxVelocity = 80.0f / 3.6f;

            float? theDistToGoal = null;

            switch (_state)
            {
                case State.IDLE:
                    _state = State.EN_ROUTE;
                    break;

                case State.EN_ROUTE:
                    // Get current position and find nearest station
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

                            // Very rough ETA -- 30 seconds + distance over max speed
                            var estimatedSecondsToGoal = 30 + distance / maxVelocity;
                            _eta = DateTime.Now + TimeSpan.FromSeconds(estimatedSecondsToGoal);        // FIXME: sim time instead of real time
                            _planStation = station;
                            _plan = plan;

                            _log.Feed(_logPin, $"Set goal: station {station.Name}, {distance:F0} m away, ETA {_eta:HH:mm:ss}");
                        }
                        else
                        {
                            _log.Feed(_logPin, $"Cannot find any station; segmentId={segmentId} t={t} dir={dir}");
                            // FIXME: There is no throttle on calling FindNearestStationAlongTrack!
                        }
                    }

                    while (_plan != null)
                    {
                        var current = _plan[0];

                        if (current.SegmentId != segmentId)
                        {
                            if (_plan.Length > 1)
                            {
                                _plan = _plan.Skip(1).ToArray(); // FIXME: code smelly smell
//                                _log.Feed(_logPin, $"Plan advanced; dtt at entry, exit: {current.DistToGoalAtEntry} {current.DistToGoalAtExit}; " +
//                                                   $"unit segmentId={segmentId} t={t} dir={dir}");
                            }
                            else
                            {
                                _plan = null;
                                _log.Feed(_logPin, "Plan exhausted");

                                // check if we really are where we should be
                                // TODO: replace this shitcode with something that makes sense
                                var nearest = _network.FindNearestStationAlongTrack(segmentId, t, dir, false);
                                if (nearest != null)
                                {
                                    var (station, stop, distance, plan) = nearest.Value;
                                    if (station.Name != _planStation.Name)        // FIXME: comparing names is a temporary work-around for asshole SqliteSimDatabase
                                    {
                                        _log.Feed(_logPin, $"Approaching {_planStation.Name}, stopping. Next station will be {station.Name}, {distance:F0} meters.");
                                        _state = State.APPROACHING;
                                        commandedVelocity = 0.0f;
                                        break;
                                    }
                                }
                            }

                            // Restart plan execution at new segment
                            continue;
                        }

                        var distAtEntry = current.DistToGoalAtEntry;
                        // The following caluclation is incorrect, because it assumes that distAtEntry == distance at segment endpoint opposite of exit.
                        // However, if the plan was made while we were already on this segment, distAtEntry will be the distance from our initial position
                        var distToGoal = current.Dir switch
                        {
                            SegmentEndpoint.End => distAtEntry - t * seg.GetLength(),
                            SegmentEndpoint.Start => distAtEntry - (1 - t) * seg.GetLength(),
                        };

                        if (distToGoal <= 0)
                        {
                            _log.Feed(_logPin, "GOAL REACHED OR MISSED with wrong segmentId ?");
                            // We have reached or missed the goal
                            // TODO: log predicted vs arrived time

                            if (_plan.Length > 1)
                            {
                                _plan = _plan.Skip(1).ToArray(); // FIXME: code smelly smell and DRY
                            }
                            else
                            {
                                _plan = null;

                                _state = State.APPROACHING;
                                commandedVelocity = 0.0f;
                                break;
                            }

                            continue;
                        }

                        if (_state != State.EN_ROUTE)
                        {
                            break;
                        }

                        theDistToGoal = distToGoal;

                        // Control loop towards objective
                        // The curve is like this:
                        //  - less than 1 meter away -> target speed 1 m/s
                        //  - less than 100 meters away -> target speed ramp to 10 m/s
                        //  - less than 740 meters away: target speed ramp to 80 km/h, and saturate there
                        // Note that at the moment, we currently need to pass the goal, before route to the next stop is calculated.
                        if (distToGoal < 1)
                        {
                            commandedVelocity = 1.0f;
                        }
                        else if (distToGoal < 100)
                        {
                            commandedVelocity = 1.0f + (distToGoal - 1) * (9.0f / 99.0f);
                        }
                        else
                        {
                            commandedVelocity = Math.Min(10 + (distToGoal - 100) * 0.03f, maxVelocity);
                        }

                        break;
                    }

                    break;

                case State.APPROACHING:
                    if (velocity < 0.1f)
                    {
                        _state = State.BOARDING;
                        _boardingTimer = 10.0;
                    }

                    commandedVelocity = 0;
                    break;

                case State.BOARDING:
                    _boardingTimer -= dt;

                    if (_boardingTimer <= 0)
                    {
                        _state = State.EN_ROUTE;
                    }

                    break;
            }

            int maxAccelerate = 1_000_000;
            int maxBrake = 2_000_000;

            var controller_P = 500_000;
            float accelerationForce = (commandedVelocity > velocity) ? Math.Min((commandedVelocity - velocity) * controller_P, maxAccelerate) : 0;

            float brakingForce;

            // TODO: This is all wrong. Braking force should *increase* as velocity decreases (because friction should be simulated as proportional to velocity)
            if (commandedVelocity > velocity)               // Accelerating
            {
                brakingForce = 0;
            }
            else if (commandedVelocity > Single.Epsilon)    // Slowing down
            {
                brakingForce = Math.Min((velocity - commandedVelocity) * 500_000, maxBrake);
            }
            else                                            // Full stop desired
            {
                brakingForce = maxBrake;
            }

            _log.FeedNullable(_distanceToTargetPin, theDistToGoal);
            _log.Feed(_accelerationForcePin, accelerationForce);
            _log.Feed(_brakingForcePin, brakingForce);
            _log.Feed(_segmentIdPin, segmentId);
            _log.Feed(_velocityTargetPin, commandedVelocity);
            _log.Feed(_velocityPin, velocity);
            _log.Feed(_debugPin, t);

            _lastDistanceToGoal = theDistToGoal;

            return (_unitIndex, accelerationForce, brakingForce);
        }

        public override string ToString()
        {
            var unit = _units.GetUnitByIndex(_unitIndex);

            string str = $"StationToStationAgent#{_unitIndex}: ";

            switch (_state)
            {
                case State.IDLE:
                    str += "awaiting commands";
                    break;

                case State.EN_ROUTE:
                    if (_lastDistanceToGoal != null)
                    {
                        str += $"en route to {_planStation?.Name}, {_lastDistanceToGoal:F0} meters, " +
                               $"{unit.Velocity.Length() * 3.6:F1} km/h, ETA {_eta:HH:mm:ss}";
                    }
                    else
                    {
                        str += $"en route, but lost";
                    }

                    break;

                case State.APPROACHING:
                    str += $"approaching {_planStation?.Name}, {unit.Velocity.Length() * 3.6:F1} km/h";
                    break;

                case State.BOARDING:
                    str += $"boarding at {_planStation?.Name}";
                    break;
            }

            return str;
        }
    }
}
