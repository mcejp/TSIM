using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using TSIM.RailroadDatabase;

using RabbitMQ.Client;

namespace TSIM.SimServer
{
    public class Program
    {
        const int simStepMs = 200;
        const int publishPeriodSteps = 5; // 5*5;

        public static void Main(string[] args)
        {
            // Doing this "properly" is super crap. (Why again?)
            string workDir = File.Exists("work/simdb.sqlite") ? "work" : "../work";

            // 0. init internals
            using var log = new LoggingManager(Path.Join(workDir, "simlog.csv"));
            var cp = new LoggingManager.ClassPolicy(acceptByDefault: false, acceptId: new int[] {0});
//            cp.SetThrottleRate(1);
            // log.SetClassPolicy(typeof(StationToStationAgent), cp);

            // 1. open pre-initialized DB
            var db = SqliteSimDatabase.Open(Path.Join(workDir, "simdb.sqlite"));

            // 2. simulate
            var sim = new Simulation(db.GetCoordinateSpace(), db, db, log);

            // 3. add agents
            for (int unitIndex = 0; unitIndex < db.GetNumUnits(); unitIndex++)
            {
                // FIXME: StationToStationAgent will be extremely slow if there are no easily reachable stations
                //sim.AddAgent(new StationToStationAgent(db, db, log, unitIndex));
            }

            // Init RabbitMQ
            var factory = new ConnectionFactory() { HostName = "localhost" };
            var connection = factory.CreateConnection();
            var channel = connection.CreateModel();

            channel.QueueDeclare(queue: "UnitDatabase_full.json",
                                 durable: false,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

            channel.QueueDeclare(queue: "TrainControl_full.json",
                                 durable: false,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);
            // End Init RabbitMQ

            var eh = log.GetEntityHandle(typeof(Program), -1);
            var perfPin = log.GetSignalPin(eh, "timeUtilization");

            var sw = new Stopwatch();

            var lastReport = DateTime.Now;
            var simTimeSinceLastReportMs = 0;
            long realTimeSinceLastReportMs = 0;

            for (int simStep = 0; ; simStep++)
            {
                sw.Restart();
                sim.Step(simStepMs * 0.001);
                sw.Stop();

                var realTimeMs = sw.ElapsedMilliseconds;

                simTimeSinceLastReportMs += simStepMs;
                realTimeSinceLastReportMs += realTimeMs;

                if (DateTime.Now > lastReport + TimeSpan.FromSeconds(1))
                {
//                    Console.WriteLine($"Took {realTimeSinceLastReportMs * 0.001:F2} s to simulate {simTimeSinceLastReportMs * 0.001:F2} s");
                    log.Feed(perfPin, (float) realTimeSinceLastReportMs / simTimeSinceLastReportMs);

                    lastReport = DateTime.Now;
                    simTimeSinceLastReportMs = 0;
                    realTimeSinceLastReportMs = 0;
                }

                if (simStep % publishPeriodSteps == 0)
                {
                    var unitsSnapshot = sim.Units.SnapshotFullMake();

                    channel.BasicPublish(exchange: "",
                                         routingKey: "UnitDatabase_full.json",
                                         basicProperties: null,
                                         body: unitsSnapshot);

                    var controllerMap = sim.GetControllerStateSummary();
                    var controlSnapshot = Serialization.SerializeTrainControlStateToJsonUtf8Bytes(controllerMap);

                    channel.BasicPublish(exchange: "",
                                         routingKey: "TrainControl_full.json",
                                         basicProperties: null,
                                         body: controlSnapshot);
                }

                var sleepTimeMs = simStepMs - realTimeMs;

                if (sleepTimeMs > 0)
                {
                    Thread.Sleep((int) sleepTimeMs);
                }
            }

            // TODO: save end state
        }
    }
}
