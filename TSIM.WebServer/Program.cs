using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public static Simulation uglyGlobalSimulation;

        public static void Main(string[] args)
        {
            // 1. open pre-initialized DB
            var db = SqliteSimDatabase.Open("../simdb.sqlite");

            // 2. simulate
            var sim = new Simulation(db.GetCoordinateSpace(), db, db);

            uglyGlobalSimulation = sim;
            Task.Run(() => Simulate(sim));

            // now start web server
            CreateHostBuilder(args).Build().Run();
        }

        private static void Simulate(Simulation sim)
        {
            const int simStepMs = 100;

            for (;;)
            {
                lock (sim)
                {
                    sim.Step(simStepMs * 0.001);
                }

                Thread.Sleep(simStepMs);
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
    }
}
