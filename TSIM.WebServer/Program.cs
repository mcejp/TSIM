using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TSIM.RailroadDatabase;

namespace TSIM.WebServer
{
    public class Program
    {
        public static Simulation uglyGlobalSimulation;        // Why global?

        public static void Main(string[] args)
        {
            // 1. open pre-initialized DB
            // Doing this "properly" is super crap. (Why again?)
            var db = SqliteSimDatabase.Open(File.Exists("work/simdb.sqlite") ? "work/simdb.sqlite" : "../work/simdb.sqlite");

            // 2. simulate
            var sim = new Simulation(db.GetCoordinateSpace(), db, db);

            // 3. add agents
            for (int unitIndex = 0; unitIndex < db.GetNumUnits(); unitIndex++)
            {   
                // Too slow for now, use constant speed
//                sim.AddAgent(new StationToStationAgent(db, db, unitIndex, unitIndex == 0));
                sim.Units.SetUnitSpeed(0, 50 / 3.6f);
            }

            uglyGlobalSimulation = sim;
            Task.Run(() => Simulate(sim));

            // now start web server
            CreateHostBuilder(args).Build().Run();
        }

        private static void Simulate(Simulation sim)
        {
            const int simStepMs = 1000;

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

                if (DateTime.Now > lastReport + TimeSpan.FromSeconds(10))
                {
                    Console.WriteLine($"Took {realTimeSinceLastReportMs * 0.001:F2} s to simulate {simTimeSinceLastReportMs * 0.001:F2} s");
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
