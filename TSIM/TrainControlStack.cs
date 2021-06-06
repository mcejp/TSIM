using System;
using System.Linq;
using System.Text.Json.Serialization;
using TSIM.Model;
using TSIM.RailroadDatabase;

namespace TSIM {

// This class contains a distillation of all the state for presentation purposes (display)
public class TrainControlStateSummary {
    public class SegmentToFollow {
        [JsonPropertyName("segmentId")]
        public int SegmentId { get; set; }
        [JsonPropertyName("entryEp")]
        public SegmentEndpoint EntryEp { get; set; }
        [JsonPropertyName("segmentLength")]
        public float SegmentLength { get; set; }
        [JsonPropertyName("goalT")]
        // TODO: should be, like, nullable or something
        public float GoalT { get; set; }
    };

    [JsonPropertyName("schedulerMode")]
    public string? SchedulerMode { get; set; }

    [JsonPropertyName("schedulerState")]
    public string? SchedulerState { get; set; }

    // speed, scheduling mode ...

    [JsonPropertyName("segmentsToFollow")]
    public SegmentToFollow[]? SegmentsToFollow { get; set; }

    public string? WaypointControllerState { get; set; }
    public string? TractionControllerState { get; set; }
}

// Wrapper for all the different controllers needed for a train.
// This way the specific control architecture can be opaque to the simulation engine.
public class TrainControlStack {
    private ScheduleController.Mode _scheduleControllerInput = ScheduleController.Mode.STOP;
    private readonly ScheduleController _scheduleController;
    private readonly WaypointController _waypointController;
    private readonly TractionController _tractionController;

    private TractionControllerCommand? _latestRtcCommand;

    // private readonly LoggingManager _log;
    // private readonly int _infoPin;

    public TrainControlStack(int unitId, LoggingManager log, INetworkDatabase network) {
        // _log = log;
        // var eh = _log.GetEntityHandle(typeof(TrainControlStack), unitId);
        // _infoPin = _log.GetSignalPin(eh, "info");

        _scheduleController = new ScheduleController(unitId, log);
        _waypointController = new WaypointController(unitId, log, network);
        _tractionController = new TractionController(unitId, log);
    }

    public (int, SegmentEndpoint)? GetPreferredContinuationSegment(int fromSegmentId, SegmentEndpoint fromEp) {
        return _tractionController.GetPreferredContinuationSegment(fromSegmentId, fromEp);
    }

    public TrainControlStateSummary GetStateSummary() => new TrainControlStateSummary {
        SchedulerMode = this.GetModeString(),
        SchedulerState = this.GetStateString(),

        // TODO: how can we avoid all of this ugly conversion?
        // Perhaps the problem is that we insist on using annotation-based serialization to JSON, so we have to duplicate all structures
        SegmentsToFollow = _latestRtcCommand?.segmentsToFollow?.Select(tuple => new TrainControlStateSummary.SegmentToFollow {
                SegmentId = tuple.segmentId, EntryEp = tuple.entryEp, SegmentLength = tuple.segmentLength, GoalT = tuple.goalT,
            }).ToArray(),

        WaypointControllerState = _waypointController.GetStatus().State.ToString(),
        TractionControllerState = _tractionController.GetState().ToString(),
        };

    private string GetModeString() => _scheduleControllerInput.ToString();

    private string GetStateString() => _scheduleController.GetState().ToString();

    public void GoAutoSchedule() {
        _scheduleControllerInput = ScheduleController.Mode.AUTO_SCHEDULE;
    }

    public float Update(double dt, DateTime simTime, TrainStatus trainStatus) {
        var wpcStatus = _waypointController.GetStatus();
        var maybeWpcCommand = _scheduleController.Update(simTime, _scheduleControllerInput, wpcStatus);

        var tcState = _tractionController.GetState();
        var rtcCommand = _waypointController.Update(simTime, maybeWpcCommand, trainStatus, tcState);
        _latestRtcCommand = rtcCommand;

        float maxVelocity = 80.0f / 3.6f;
        float maxAccel = 1.0f;
        float maxDecel = 1.3f;
        var acceleration = _tractionController.Update(dt, rtcCommand, trainStatus, maxVelocity, maxAccel, maxDecel);

        return acceleration;
    }
}

}
