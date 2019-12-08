using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using TSIM.RailroadDatabase;

namespace TSIM.WebServer
{
    public class Program
    {
        public static Simulation uglyGlobalSimulation;        // Global because HomeController uses it

        private readonly LoggingManager _log;
        private readonly int _perfPin;

        public Program(LoggingManager log)
        {
            _log = log;

            var eh = log.GetEntityHandle(typeof(Program), -1);
            _perfPin = log.GetSignalPin(eh, "timeUtilization");
        }

        public static void Main(string[] args)
        {
            // Doing this "properly" is super crap. (Why again?)
            string workDir = File.Exists("work/simdb.sqlite") ? "work" : "../work";

            // 0. init internals
            using var log = new LoggingManager(Path.Join(workDir, "simlog.csv"));
            var cp = new LoggingManager.ClassPolicy(acceptByDefault: false, acceptId: new int[] {0});
//            cp.SetThrottleRate(1);
            log.SetClassPolicy(typeof(StationToStationAgent), cp);

            // 1. open pre-initialized DB
            var db = SqliteSimDatabase.Open(Path.Join(workDir, "simdb.sqlite"));

            // 2. simulate
            var sim = new Simulation(db.GetCoordinateSpace(), db, db, log);

            // 3. add agents
            for (int unitIndex = 0; unitIndex < db.GetNumUnits(); unitIndex++)
            {
                // FIXME: StationToStationAgent will be extremely slow if there are no easily reachable stations
                sim.AddAgent(new StationToStationAgent(db, db, log, unitIndex));

                // Backup:
//                sim.Units.SetUnitSpeed(0, 50 / 3.6f);
            }

            uglyGlobalSimulation = sim;

            var p = new Program(log);
            Task.Run(() => p.Simulate(sim));

            // now start web server
            CreateHostBuilder(args).Build().Run();
        }

        // TODO: maybe factor this out into a "RealtimeSimulationContext" or something
        private void Simulate(Simulation sim)
        {
            const int simStepMs = 200;

            var sw = new Stopwatch();

            var lastReport = DateTime.Now;
            var simTimeSinceLastReportMs = 0;
            long realTimeSinceLastReportMs = 0;

            for (;;)
            {
                lock (sim)
                {
                    sw.Restart();
                    sim.Step(simStepMs * 0.001);
                    sw.Stop();
                }

                var realTimeMs = sw.ElapsedMilliseconds;

                simTimeSinceLastReportMs += simStepMs;
                realTimeSinceLastReportMs += realTimeMs;

                if (DateTime.Now > lastReport + TimeSpan.FromSeconds(1))
                {
//                    Console.WriteLine($"Took {realTimeSinceLastReportMs * 0.001:F2} s to simulate {simTimeSinceLastReportMs * 0.001:F2} s");
                    _log.Feed(_perfPin, (float) realTimeSinceLastReportMs / simTimeSinceLastReportMs);

                    lastReport = DateTime.Now;
                    simTimeSinceLastReportMs = 0;
                    realTimeSinceLastReportMs = 0;
                }

                var sleepTimeMs = simStepMs - realTimeMs;

                if (sleepTimeMs > 0)
                {
                    Thread.Sleep((int) sleepTimeMs);
                }
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
    }
}
