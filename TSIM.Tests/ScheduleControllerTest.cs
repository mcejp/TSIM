// https://docs.microsoft.com/en-us/visualstudio/test/unit-test-basics?view=vs-2017

using System;
using TSIM;
using Xunit;

namespace TSIM.Tests
{
    public class ScheduleControllerTest
    {
        [Fact]
        public void TestScheduleController()
        {
            var log = new LoggingManager("/dev/null");

            var controller = new ScheduleController(1, log);

            controller.SetSchedule(new[] {
                new ScheduleEntry{StationId = 1, MinimumBoardingTime = TimeSpan.FromMinutes(2),
                                  ArrivalTime = new DateTime(2020, 09, 12, 12, 00, 00), DepartureTime = new DateTime(2020, 09, 12, 12, 02, 00)},
                new ScheduleEntry{StationId = 2, MinimumBoardingTime = TimeSpan.FromMinutes(2),
                                  ArrivalTime = new DateTime(2020, 09, 12, 12, 10, 00), DepartureTime = new DateTime(2020, 09, 12, 12, 12, 00)},
            });

            var cmd = controller.Update(new DateTime(2020, 09, 12, 12, 00, 00), ScheduleController.Mode.FOLLOW_SCHEDULE,
                    new WaypointControllerStatus{State = WaypointController.State.STOPPED});

            Assert.NotNull(cmd);
            Assert.Equal(WaypointController.Mode.GOTO_STATION, cmd.Value.mode);
            Assert.Equal(1, cmd.Value.gotoStationId);
        }
    }
}
