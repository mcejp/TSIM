using System;
using TSIM.Model;
using TSIM.RailroadDatabase;

namespace TSIM {

// Wrapper for all the different controllers needed for a train.
// This way the specific control architecture can be opaque to the simulation engine.
public class TrainControlStack {
    private ScheduleController.Mode _scheduleControllerInput = ScheduleController.Mode.STOP;

    private readonly ScheduleController _scheduleController;
    private readonly WaypointController _waypointController;
    private readonly TractionController _tractionController;

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

    public string GetStateString() => _scheduleController.GetState().ToString();

    public void GoAutoSchedule() {
        _scheduleControllerInput = ScheduleController.Mode.AUTO_SCHEDULE;
    }

    public float Update(double dt, DateTime simTime, TrainStatus trainStatus) {
        var wpcStatus = _waypointController.GetStatus();
        var maybeWpcCommand = _scheduleController.Update(simTime, _scheduleControllerInput, wpcStatus);

        var tcState = _tractionController.GetState();
        var rtcCommand = _waypointController.Update(simTime, maybeWpcCommand, trainStatus, tcState);

        float maxVelocity = 80.0f / 3.6f;
        float maxAccel = 1.0f;
        float maxDecel = 1.3f;
        var acceleration = _tractionController.Update(dt, rtcCommand, trainStatus, maxVelocity, maxAccel, maxDecel);

        return acceleration;
    }
}

}
