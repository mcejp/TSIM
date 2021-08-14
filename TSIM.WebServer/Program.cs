using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;

using PeterO.Cbor;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

using TSIM.RailroadDatabase;

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

            var simStateQueue = channel.QueueDeclare().QueueName;
            channel.ExchangeDeclare(exchange: "SimState_full.cbor",
                                    type: ExchangeType.Fanout);
            channel.QueueBind(queue: simStateQueue,
                              exchange: "SimState_full.cbor",
                              routingKey: "");

            channel.ExchangeDeclare(exchange: "TSIM.cbor",
                                    type: ExchangeType.Fanout);

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                // var message = Encoding.UTF8.GetString(ea.Body.ToArray());
                // Console.WriteLine("RX " + message);

                var (unitsSnapshot, controlSnapshot, simInfoSnapshot) = Serialization.UnglueFullSimSnapshot(ea.Body.ToArray());

                // var message = Encoding.UTF8.GetString(unitsSnapshot);
                // Console.WriteLine("RX " + message);

                // var message = Encoding.UTF8.GetString(controlSnapshot);
                // Console.WriteLine("RX " + message);

                var controlStateMap = Serialization.DeserializeTrainControlState(controlSnapshot);

                uglyGlobalTCSS = controlStateMap;

                lock (sim)
                {
                    Serialization.DeserializeInfoSnapshot(sim, simInfoSnapshot);
                    sim.Units.SnapshotFullRestore(unitsSnapshot);

                    // Render outputs

                    var filename = "/tmp/tmp.png";
                    var w = 1400;
                    var h = 1000;
                    var scale = 0.150;          // TODO: automatically determine boundaries of view
                    var fontSize = 9;
                    GraphicsOutput.RenderPng(sim.CoordSpace, sim.Network, sim.Units, Program.uglyGlobalTCSS, filename, w, h, scale, fontSize);

                    byte[] filedata = System.IO.File.ReadAllBytes(filename);
                    string contentType = "image/png";

                    // Wrap in CBOR & publish

                    var cbor = CBORObject.NewMap()
                        .Add("objects", CBORObject.NewArray()
                            .Add(CBORObject.NewMap()
                                .Add("name", "main")    // deprecated
                                .Add("topic", "main")
                                .Add("displayName", "TSIM View")
                                .Add("mimeType", contentType)
                                .Add("data", filedata)
                            )
                            .Add(ControlSystemStateMapToCbor(Program.uglyGlobalTCSS))
                            .Add(SimulationStateMapToCbor(sim.SimTimeElapsed))
                        )
                        .Add("controls", CBORObject.NewArray())
                        ;

                    channel.BasicPublish(exchange: "TSIM.cbor",
                                         routingKey: "",
                                         basicProperties: null,
                                         body: cbor.EncodeToBytes());
                }
            };

            channel.BasicConsume(queue: simStateQueue,
                                  autoAck: true,
                                  consumer: consumer);
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });

        private static CBORObject ControlSystemStateMapToCbor(IDictionary<int, TrainControlStateSummary> map) {
            var cborMap = CBORObject.NewMap();

            foreach (var entry in map) {
                var repr = CBORObject.NewMap()
                    .Add("schedulerMode", entry.Value.SchedulerMode)
                    .Add("schedulerState", entry.Value.SchedulerState)
                    .Add("schedule", entry.Value.Schedule)
                    .Add("numSegmentsToFollow", entry.Value.SegmentsToFollow?.Length)
                    .Add("waypointControllerState", entry.Value.WaypointControllerState)
                    .Add("tractionControllerState", entry.Value.TractionControllerState)
                    ;

                cborMap.Add(entry.Key.ToString(), repr);
            }

            return CBORObject.NewMap()
                .Add("name", "control-system-state")    // deprecated
                .Add("topic", "control-system-state")
                .Add("displayName", "Control System State")
                .Add("mimeType", "TSIM.ControlSystemStateMap")
                .Add("data", cborMap)
                ;
        }

        private static CBORObject SimulationStateMapToCbor(TimeSpan simTimeElapsed) {
            var cborMap = CBORObject.NewMap();

            cborMap.Add("simTimeElapsed", simTimeElapsed.ToString());

            return CBORObject.NewMap()
                .Add("name", "simulation-state")    // deprecated
                .Add("topic", "simulation-state")
                .Add("displayName", "Simulation state")
                .Add("mimeType", "application/json")
                .Add("data", cborMap)
                ;
        }
    }
}
