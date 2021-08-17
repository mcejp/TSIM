using System;
using System.Collections.Generic;
using System.Linq;

namespace TSIM {

public struct ScheduleEntry {
    public int StationId;
    //int trackNo;
    public TimeSpan MinimumBoardingTime;
    public DateTime ArrivalTime;        // time of day
    public DateTime DepartureTime;      // time of day

    // public ScheduleEntry(int stationId, TimeSpan minimumBoardingTime, DateTime arrivalTime, DateTime departureTime) {
    //     StationId = stationId;
    //     MinimumBoardingTime = minimumBoardingTime;
    //     ArrivalTime = arrivalTime;
    //     DepartureTime = departureTime;
    // }

    public override string ToString() =>
        $"(station={StationId} arrival={ArrivalTime.ToLongTimeString()} depart={DepartureTime.ToLongTimeString()} boarding={MinimumBoardingTime})";
}

public class ScheduleController {
    public enum Mode {
        STOP,
        FOLLOW_SCHEDULE,
        AUTO_SCHEDULE,
    }

    public enum State {
        STOPPED,
        BOARDING,
        BOARDING_COMPLETE,
        EN_ROUTE,
        GOTO_NEAREST_STATION,
        STOPPING,
        NO_SCHEDULE,
        NO_ROUTE,
    }

    private static readonly TimeSpan boardingTimeInAutoScheduleMode = TimeSpan.FromSeconds(10);

    private State _state = State.STOPPED;
    private List<ScheduleEntry> _schedule = new();
    private int _schedulePos;
    private DateTime? _boardingEndTime;

    private readonly LoggingManager _log;
    private readonly int _infoPin, _statePin, _schedulePosPin;

    public ScheduleController(int unitId, LoggingManager log)
    {
        _log = log;
        var eh = _log.GetEntityHandle(this.GetType(), unitId);
        _infoPin = _log.GetSignalPin(eh, "info");
        _statePin = _log.GetSignalPin(eh, "state");
        _schedulePosPin = _log.GetSignalPin(eh, "schedulePos");
    }

    public ScheduleEntry[] GetSchedule() => _schedule.ToArray();

    public State GetState() => _state;

    public WaypointControllerCommand? Update(DateTime simTime, Mode input, WaypointControllerStatus wpcStatus) {
        WaypointControllerCommand? wpc = null;

        switch (input) {
            case Mode.STOP:
                if (_state != State.STOPPING && _state != State.STOPPED) {
                    wpc = WaypointControllerCommand.STOP;
                    _state = State.STOPPING;
                }
                break;

            case Mode.AUTO_SCHEDULE:
                switch (_state) {
                    case State.NO_SCHEDULE:
                    case State.STOPPED:
                    case State.BOARDING_COMPLETE:
                    case State.EN_ROUTE:
                        if (_state == State.BOARDING_COMPLETE) {
                            // TODO: might want to update departure time
                        }

                        wpc = new WaypointControllerCommand { mode = WaypointController.Mode.GOTO_NEAREST_STATION };
                        _state = State.GOTO_NEAREST_STATION;
                        break;

                    case State.GOTO_NEAREST_STATION:
                        if (wpcStatus.State == WaypointController.State.ARRIVED) {
                            // Update arrival time + add station to auto-schedule
                            int stationArrived = wpcStatus.ArrivedAtStation.Value;

                            // TODO: bound schedule length
                            _schedule.Add(new ScheduleEntry {
                                ArrivalTime = simTime,
                                DepartureTime = simTime + boardingTimeInAutoScheduleMode,
                                MinimumBoardingTime = boardingTimeInAutoScheduleMode,
                                StationId = stationArrived,
                            });

                            _boardingEndTime = simTime + boardingTimeInAutoScheduleMode;
                            _state = State.BOARDING;
                        }
                        break;

                    case State.BOARDING:
                        if (simTime >= _boardingEndTime) {
                            _state = State.BOARDING_COMPLETE;
                        }

                        break;

                    default:
                        break;
                }
                break;

            case Mode.FOLLOW_SCHEDULE:
                switch (_state) {
                    case State.NO_SCHEDULE:
                    case State.STOPPED:
                        // TODO: might need to update departure time
                        wpc = Go();
                        break;

                    case State.EN_ROUTE:
                        if (wpcStatus.State == WaypointController.State.ARRIVED) {
                            // need to board? let's say yes
                            _boardingEndTime = new[] { _schedule[_schedulePos].DepartureTime,
                                                       simTime + _schedule[_schedulePos].MinimumBoardingTime }.Max();
                            _state = State.BOARDING;
                        }

                        break;
                    
                    case State.BOARDING:
                        if (simTime >= _boardingEndTime) {
                            _state = State.BOARDING_COMPLETE;
                        }

                        break;
                    
                    case State.BOARDING_COMPLETE:
                        _schedulePos += 1;
                        wpc = Go();
                        break;
                }
                break;
        }

        _log.Feed(_statePin, _state.ToString());
        _log.Feed(_schedulePosPin, _schedulePos);

        return wpc;
    }

    private WaypointControllerCommand? Go() {
        // check schedule -> how do we tell whether to start boarding or going to next station?
        // do we have a schedule ?

        if (_schedule.Count == 0) {
            if (_state != State.NO_SCHEDULE) {
                _log.Feed(_infoPin, "Cannot start navigation -- no orders in schedule");
                _state = State.NO_SCHEDULE;
                return WaypointControllerCommand.STOP;
            }
            else {
                return null;
            }
        }

        if (_schedulePos >= _schedule.Count) {
            _schedulePos = 0;
        }

        var next = _schedule[_schedulePos];

        // set state to go to next station
        _state = State.EN_ROUTE;

        return new WaypointControllerCommand { mode = WaypointController.Mode.GOTO_STATION, gotoStationId = _schedule[_schedulePos].StationId };
    }

    public void SetSchedule(ScheduleEntry[] scheduleEntries) {
        _schedule = new(scheduleEntries);
        _schedulePos = 0;
        _state = State.EN_ROUTE;
    }
}

}
