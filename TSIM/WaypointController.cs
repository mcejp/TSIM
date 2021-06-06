using System;
using TSIM.Model;
using TSIM.RailroadDatabase;

namespace TSIM {

public struct WaypointControllerStatus {
    public WaypointController.State State;
    public int? ArrivedAtStation;
}

public struct WaypointControllerCommand {
    public WaypointController.Mode mode;

    public int? gotoStationId;

    public static readonly WaypointControllerCommand STOP = new WaypointControllerCommand { mode = WaypointController.Mode.STOP, gotoStationId = null };
}

public class WaypointController {
    public enum Mode {
        STOP,
        GOTO_STATION,
        GOTO_NEAREST_STATION,
    }

    public enum State {
        STOPPED,
        PLANNING,
        EN_ROUTE,
        STOPPING,
        ARRIVED,

        NO_PATH,
    }

    int? _currentPlanForStationId = null;
    private int? _lastStationArrivedAt = null;
    int? _lastStationGoneTo = null; // This is saved only to prevent GoToNearestStation to be "stuck" at the same station
    TractionControllerCommand? _currentPlanCommand = null;
    State _state = State.STOPPED;

    private readonly INetworkDatabase _network;
    private readonly RoutePlanner _routePlanner;

    private readonly LoggingManager _log;
    private readonly int _infoPin, _statePin;

    public WaypointController(int unitId, LoggingManager log, INetworkDatabase network)
    {
        _network = network;
        _routePlanner = new RoutePlanner(network);

        _log = log;
        var eh = _log.GetEntityHandle(this.GetType(), unitId);
        _infoPin = _log.GetSignalPin(eh, "info");
        _statePin = _log.GetSignalPin(eh, "state");
    }

    public WaypointControllerStatus GetStatus()
    {
        return new WaypointControllerStatus { State = this._state, ArrivedAtStation = this._lastStationArrivedAt };
    }

    // WaypointControllerCommand is NOT idempotent, because we want to for example recognize two separate GOTO_NEAREST_STATION commands
    // Tried before: idempotent commands, nested switches... it was a mess
    public TractionControllerCommand? Update(DateTime simTime, WaypointControllerCommand? maybeCommand, TrainStatus trainStatus, TractionController.State tcState) {
        // Process any commands
        if (maybeCommand.HasValue) {
            var command = maybeCommand.Value;

            switch (command.mode) {
                case Mode.STOP:
                    Stop();
                    break;

                case Mode.GOTO_STATION:
                    GoToStation(command.gotoStationId.Value, trainStatus);
                    break;

                case Mode.GOTO_NEAREST_STATION:
                    GoToNearestStation(trainStatus, _lastStationGoneTo);
                    break;
            }
        }

        // State update
        switch (_state) {
            case State.STOPPING:
                // TODO: check if finished stopping -> go to state STOPPED
                break;

            case State.EN_ROUTE:
                if (tcState == TractionController.State.IN_DESTINATION) {
                    // (This assumes that the current plan goes all the way to destination)
                    _state = State.ARRIVED;
                    _lastStationArrivedAt = _currentPlanForStationId;
                }
                break;
        }

        _log.Feed(_statePin, _state.ToString());

        // Output
        switch (_state) {
            case State.NO_PATH:
            case State.STOPPED:
            case State.STOPPING:
            case State.PLANNING:
            case State.ARRIVED:
                return TractionControllerCommand.STOP;

            case State.EN_ROUTE:
                return _currentPlanCommand.Value;

            default:
                throw new InvalidOperationException();
        }
    }

    private void GoToNearestStation(TrainStatus trainStatus, int? excludedStationId) {
        var (segmentId, t, dir) = (trainStatus.SegmentId, trainStatus.T, trainStatus.Dir);

        var nearest = _network.FindNearestStationAlongTrack(segmentId, t, dir, excludedStationId, false);

        if (nearest != null)
        {
            var (station, stop, distance, plan) = nearest.Value;

            // Very rough ETA -- 30 seconds + distance over max speed
            // var estimatedSecondsToGoal = 30 + distance / maxVelocity;
            // _eta = DateTime.Now + TimeSpan.FromSeconds(estimatedSecondsToGoal);        // FIXME: sim time instead of real time
            // _planStation = station;
            // _plan = plan;

            _log.Feed(_infoPin, $"Set goal: station {station.Name}, {distance:F0} m away"); // , ETA {_eta:HH:mm:ss}

            GoToStation(station.Id, trainStatus);
        }
        else
        {
            _log.Feed(_infoPin, $"GOTO_NEAREST_STATION: Cannot find any station; segmentId={segmentId} t={t} dir={dir}");
            _state = State.NO_PATH;
        }
    }

    private void GoToStation(int stationId, TrainStatus trainStatus) {
        // find destination station
        var station = _network.GetStationById(stationId);
        _log.Feed(_infoPin, $"Planning route to station {station.Name}");

        // FIXME: wrong! correct to set this _only_ after arrival confirmation!
        _lastStationGoneTo = stationId;

        // FIXME: at the moment, this will only try the first stop of the station
        foreach (var stop in station.Stops) {
            int destinationSegmentId = stop.SegmentId;
            float destinationT = stop.T;

            var result = _routePlanner.PlanRoute(trainStatus.SegmentId, trainStatus.T, trainStatus.Dir, destinationSegmentId, destinationT);

            if (result != null) {
                var currentSegmentLength = _network.GetSegmentById(trainStatus.SegmentId).GetLength();

                _currentPlanForStationId = stationId;
                _currentPlanCommand = new TractionControllerCommand{ segmentsToFollow = new (int, SegmentEndpoint, float, float)[1 + result.route.Length] };
                _currentPlanCommand.Value.segmentsToFollow[0] = (trainStatus.SegmentId, trainStatus.Dir.Other(), currentSegmentLength, -1.0f);
                for (int i = 0; i < result.route.Length; i++) {
                    var entry = result.route[i];
                    _currentPlanCommand.Value.segmentsToFollow[1 + i] = entry;
                }

                // PrintPlan(_currentPlanCommand.Value);

                _state = State.EN_ROUTE;

                _log.Feed(_infoPin, $"OK route to station {station.Name}, distance {result.totalCost} m");
            }
            else {
                _state = State.NO_PATH;
                _currentPlanForStationId = stationId;

                Console.WriteLine("TODO: shit...");
                _log.Feed(_infoPin, $"NO ROUTE to station {station.Name}");
            }

            return;
        }

        throw new ApplicationException("Goal station has no stops!");
    }

    private void PrintPlan(TractionControllerCommand command) {
        Console.WriteLine("PRINTPLAN:");

        for (int i = 0; i < command.segmentsToFollow.Length; i++) {
            var entry = command.segmentsToFollow[i];
            if (i == 0) {
                Console.WriteLine($" -- (Segment {entry.segmentId} for {entry.segmentLength,6:F2} m from current position; entry {entry.entryEp}; t_goal {entry.goalT})");
            }
            else {
                Console.WriteLine($" -- (Segment {entry.segmentId} of {entry.segmentLength,6:F2} m total; entry {entry.entryEp}; t_goal {entry.goalT})");
            }
        }
    }

    private void Stop() {
        _state = State.STOPPING;
    }

}

}
