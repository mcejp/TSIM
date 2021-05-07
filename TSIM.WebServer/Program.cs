using System.IO;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

using TSIM.RailroadDatabase;
using System;

namespace TSIM.WebServer
{
    public class Program
    {
        public static Simulation uglyGlobalSimulation;        // Global because HomeController uses it
                                                              // TODO: Obliterate this!

        // No locking required, replaced atomically
        public static IDictionary<int, TrainControlStateSummary> uglyGlobalTCSS;

        public static void Main(string[] args)
        {
            // Doing this "properly" is super crap. (Why again?)
            string workDir = File.Exists("work/simdb.sqlite") ? "work" : "../work";

            // 0. init internals
            using var log = new LoggingManager(Path.Join(workDir, "simlog_dummy.csv"));

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

            uglyGlobalSimulation = sim;

            Subscribe(sim);

            // now start web server
            CreateHostBuilder(args).Build().Run();
        }

        private static void Subscribe(Simulation sim)
        {
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

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                // Console.WriteLine("RX " + message);

                lock (sim)
                {
                    sim.Units.SnapshotFullRestore(body);
                }
            };

            var consumer2 = new EventingBasicConsumer(channel);
            consumer2.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                // Console.WriteLine("RX " + message);

                var controlStateMap = Serialization.DeserializeTrainControlState(body);

                uglyGlobalTCSS = controlStateMap;
            };

            channel.BasicConsume(queue: "UnitDatabase_full.json",
                                  autoAck: true,
                                  consumer: consumer);

            channel.BasicConsume(queue: "TrainControl_full.json",
                                  autoAck: true,
                                  consumer: consumer2);
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
    }
}
