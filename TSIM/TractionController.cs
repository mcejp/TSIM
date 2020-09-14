/*
 * Traction Controller
 *
 * Purpose: real-time control of the train to reach desired goals.
 *     Acting as an attorney between the higher-order controller and the physics simulation engine.
 *
 * Inputs from higher-level controller ("client"):
 *  - train physics model (such as max deceleration)
 *  - desired path (list of segment IDs)
 * Outputs:
 *  - some statistics perhaps
 */

using System;
using System.Numerics;
using TSIM.Model;

namespace TSIM {

public struct TrainStatus {
    public int SegmentId;
    public SegmentEndpoint Dir;
    public float T;

    public Vector3 Velocity;
}

public struct TractionControllerCommand {
    public (int segmentId, SegmentEndpoint entryEp, float segmentLength, float goalT)[] segmentsToFollow;

    public static readonly TractionControllerCommand STOP = new TractionControllerCommand{segmentsToFollow = new (int, SegmentEndpoint, float, float)[] {}};
}

public class TractionController {
    public enum State {
        STOPPED,
        EN_ROUTE,
        APPROACHING,
        IN_DESTINATION,
        NO_PLAN,
    }

    private State _state = State.STOPPED;
    private TractionControllerCommand? _lastCommand;
    private readonly LoggingManager _log;
    private readonly int _infoPin, _statePin, _trackAheadPin, _vMaxPin, _controlModePin, _currentIndexPin, _errorPin;

    public TractionController(int unitId, LoggingManager log)
    {
        _log = log;
        var eh = _log.GetEntityHandle(this.GetType(), unitId);
        _infoPin = _log.GetMessageSignalPin(eh, "info");
        _statePin = _log.GetSignalPin(eh, "state");
        _trackAheadPin = _log.GetSignalPin(eh, "trackAhead");
        _vMaxPin = _log.GetSignalPin(eh, "v_max");
        _controlModePin = _log.GetSignalPin(eh, "controlMode");
        _currentIndexPin = _log.GetSignalPin(eh, "currentIndex");
        _errorPin = _log.GetMessageSignalPin(eh, "error");
    }

    public (int, SegmentEndpoint)? GetPreferredContinuationSegment(int fromSegmentId, SegmentEndpoint fromEp) {
        if (_lastCommand == null) {
            return null;
        }

        var cmd = _lastCommand.Value;

        // Try to understand where in the plan we are
        int currentIndex = -1;

        for (int i = 0; i < cmd.segmentsToFollow.Length; i++) {
            if (fromSegmentId == cmd.segmentsToFollow[i].segmentId && fromEp == cmd.segmentsToFollow[i].entryEp.Other()) {
                currentIndex = i;
                break;
            }
        }

        if (currentIndex >= 0 && currentIndex + 1 < cmd.segmentsToFollow.Length) {
            // Easy -- it's the next one.
            return (cmd.segmentsToFollow[currentIndex + 1].segmentId, cmd.segmentsToFollow[currentIndex + 1].entryEp);
        }
        else {
            return null;
        }
    }

    public State GetState() => _state;

    // For the moment, rtcCommand is idempotent
    // It works like this:
    //  - The Waypoint Controller provides a list of segments to go through (and how far to go within them)
    //  - We then track the segments in the plan as they are being followed by the train
    public float Update(double dt, TractionControllerCommand? maybeCommand, TrainStatus trainStatus, float maxVelocity, float maxAccel, float maxDecel) {
        _lastCommand = maybeCommand;

        // What is the current objective?
        // var unit = _units.GetUnitByIndex(_unitIndex);
        var velocity = trainStatus.Velocity.Length();

        var (segmentId, t, dir) = (trainStatus.SegmentId, trainStatus.T, trainStatus.Dir);

        float acceleration = 0;

        // No command => stop!
        if (maybeCommand == null || maybeCommand.Value.segmentsToFollow.Length == 0) {
            acceleration = TrainModel.AccelerationToFullyStopNow(dt, velocity, maxDecel);
            _state = State.NO_PLAN;
        }
        else {
            var cmd = maybeCommand.Value;

            // Our inputs are a list of segments to pass. First check if the current one is like first or second
            // (otherwise panic)

            int currentIndex = -1;

            // FIXME: This is not so easy!! What if our plan includes passing the same segment multiple times,
            //        or for example turning around?
            for (int i = 0; i < cmd.segmentsToFollow.Length; i++) {
                if (segmentId == cmd.segmentsToFollow[i].segmentId) {
                    currentIndex = i;
                    break;
                }
            }

            _log.Feed(_currentIndexPin, currentIndex);

            if (currentIndex < 0) {
                if (_state != State.NO_PLAN) {
                    // If "newly" lost, print diagnostics
                    Console.WriteLine($"TRAIN IS LOST: currently at ({segmentId}:{t} -> {dir}), but plan is:");

                    for (int i = 0; i < cmd.segmentsToFollow.Length; i++) {
                        Console.WriteLine($" -- {cmd.segmentsToFollow[i]}");
                    }
                }
                
                acceleration = TrainModel.AccelerationToFullyStopNow(dt, velocity, maxDecel);
                _state = State.NO_PLAN;
            }
            else {
                // Now we know where we are. Determine the most conservative speed required, based on upcoming speed limits and total reserved path length

                // Count the distance remaining in the current segment

                float totalDist;

                // First of all, are there more segments beyond the current one?

                if (currentIndex + 1 < cmd.segmentsToFollow.Length) {
                    totalDist = Utility.DistanceToEndpoint(cmd.segmentsToFollow[currentIndex].segmentLength,
                                                           t,
                                                           cmd.segmentsToFollow[currentIndex].entryEp.Other());

                    for (int i = currentIndex + 1; i < cmd.segmentsToFollow.Length; i++) {
                        var item = cmd.segmentsToFollow[i];

                        if (i + 1 < cmd.segmentsToFollow.Length) {
                            totalDist += item.segmentLength;
                        }
                        else {
                            // For the final segment, consider only distance up to t_goal
                            totalDist += Utility.DistanceToEndpoint(item.segmentLength, item.goalT, item.entryEp);
                        }
                    }
                }
                else {
                    // This is the current & last segment -- remaining distance is calculated a bit differently
                    // Explicitly handle these two separately instead of just doing Abs, to help discover any subtle issues
                    if (cmd.segmentsToFollow[currentIndex].entryEp == SegmentEndpoint.Start) {
                        totalDist = (cmd.segmentsToFollow[currentIndex].goalT - t) * cmd.segmentsToFollow[currentIndex].segmentLength;
                    }
                    else {
                        totalDist = (t - cmd.segmentsToFollow[currentIndex].goalT) * cmd.segmentsToFollow[currentIndex].segmentLength;
                    }
                }

                _log.Feed(_trackAheadPin, totalDist);
                if (totalDist < 0) {
                    _log.Feed(_errorPin, $"totalDist < 0: {totalDist,8:F4}; ({segmentId}:{t} -> {dir} / -> {cmd.segmentsToFollow[currentIndex].goalT}); {currentIndex}/{cmd.segmentsToFollow.Length}");
                    Console.WriteLine($"totalDist < 0: {totalDist,8:F4}; ({segmentId}:{t} -> {dir} / -> {cmd.segmentsToFollow[currentIndex].goalT}); {currentIndex}/{cmd.segmentsToFollow.Length}");
                    //totalDist = 0;  // mask it to prevent further mess
                }
                System.Diagnostics.Debug.Assert(totalDist >= 0);

                float remainingDistanceToStartFullyBraking = 0.1f * (float)dt;

                if (totalDist > remainingDistanceToStartFullyBraking) {
                    // Control loop towards objective
                    float v_max;
                    TrainModel.CalculationMode mode;
                    (acceleration, v_max, mode) = TrainModel.AccelerationToFullyStopAfter2(velocity, totalDist, maxAccel, maxDecel, maxVelocity, (float)dt);
                    // Console.WriteLine($"({acceleration}, {v_max}) = AccelerationToFullyStopAfter2({velocity}, {totalDist}, {maxAccel}, {maxDecel}, {maxVelocity}, {(float)dt})");

                    _state = State.EN_ROUTE;
                    _log.Feed(_vMaxPin, v_max);
                    _log.Feed(_controlModePin, mode.ToString());
                }
                else if (velocity > 0.01f) {
                    acceleration = TrainModel.AccelerationToFullyStopNow(dt, velocity, maxDecel);
                    _state = State.APPROACHING;
                }
                else {
                    acceleration = -maxDecel;
                    _state = State.IN_DESTINATION;
                }
            }
        }

        _log.Feed(_statePin, _state.ToString());

        return acceleration;
    }
}

}
