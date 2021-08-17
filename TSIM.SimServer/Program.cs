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
        public static void Main(float simStep = 0.2F, int publishInterval = 5)
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

            channel.ExchangeDeclare(exchange: "SimState_full.cbor",
                                    type: ExchangeType.Fanout);

            // Init Signal log
            var eh = log.GetEntityHandle(typeof(Program), -1);
            var perfPin = log.GetSignalPin(eh, "timeUtilization");
            float? lastPerfValue = null;

            var sw = new Stopwatch();

            var lastReport = DateTime.Now;
            var simTimeSinceLastReportMs = 0;
            long realTimeSinceLastReportMs = 0;

            for (int simStepNum = 0; ; simStepNum++)
            {
                sw.Restart();
                sim.Step(simStep);
                sw.Stop();

                var realTimeMs = sw.ElapsedMilliseconds;

                simTimeSinceLastReportMs += (int)(simStep * 1000);
                realTimeSinceLastReportMs += realTimeMs;

                if (DateTime.Now > lastReport + TimeSpan.FromSeconds(1))
                {
//                    Console.WriteLine($"Took {realTimeSinceLastReportMs * 0.001:F2} s to simulate {simTimeSinceLastReportMs * 0.001:F2} s");
                    var perf = (float) realTimeSinceLastReportMs / simTimeSinceLastReportMs;
                    log.Feed(perfPin, perf);
                    lastPerfValue = perf;

                    lastReport = DateTime.Now;
                    simTimeSinceLastReportMs = 0;
                    realTimeSinceLastReportMs = 0;
                }

                if (simStepNum % publishInterval == 0)
                {
                    var unitsSnapshot = sim.Units.SnapshotFullMake();
                    var controllerMap = sim.GetControllerStateSummary();
                    var controlSnapshot = Serialization.SerializeTrainControlStateToJsonUtf8Bytes(controllerMap);
                    var simInfoSnapshot = Serialization.MakeSimInfoSnapshot(sim, lastPerfValue);

                    var fullSnapshot = Serialization.GlueFullSimSnapshot(unitsSnapshot, controlSnapshot, simInfoSnapshot);

                    channel.BasicPublish(exchange: "SimState_full.cbor",
                                         routingKey: "",
                                         basicProperties: null,
                                         body: fullSnapshot);
                    // Console.WriteLine($"Pub {sim.SimTimeElapsed}");
                }

                var sleepTimeMs = simStep * 1000 - realTimeMs;

                if (sleepTimeMs > 0)
                {
                    Thread.Sleep((int) sleepTimeMs);
                }
            }

            // TODO: save end state
        }
    }
}
