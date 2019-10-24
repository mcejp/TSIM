using CommandLine;
using System;
using System.Diagnostics;
using TSIM.RailroadDatabase;

namespace TSIM
{
    static class Program
    {
        class Options
        {
            [Value(0, MetaName = "dbfile", Required = true, HelpText = "Database file to use. (does not need to exist)")]
            public string DbFile { get; set; }

            [Option(Required = false, HelpText = "Import a scenario from JSON.")]
            public string? ImportScenario { get; set; }

            [Option(Required = false, HelpText = "Render SVG to the specified file.")]
            public string? RenderSvg { get; set; }

            // TODO: this is obviously useless and only temporary
            [Option(Required = false, HelpText = "Run simulation for a brief time.")]
            public bool Simulate { get; set; }
        }

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed<Options>(o =>
                {
                    // import GeoJSON data to SQLite
                    if (o.ImportScenario != null)
                    {
                        InitializeDatabaseFrom(o.DbFile, o.ImportScenario);
                    }

                    // open database
                    var db = SqliteSimDatabase.Open(o.DbFile);

                    // simulate
                    if (o.Simulate)
                    {
                        var sw = new Stopwatch();
                        sw.Restart();

                        var sim = new Simulation(db.GetCoordinateSpace(), db, db);
                        sim.Units.SetUnitSpeed(0, 50 / 3.6f);

                        var steps = 50;
                        var dt = 1.0f;

                        for (var i = 0; i < steps; i++)
                        {
                            sim.Step(dt);
                        }

                        Console.WriteLine($"Took {sw.ElapsedMilliseconds * 0.001:F2} s to simulate {steps * dt:F2} s");
                    }

                    // render 2D/3D view
                    if (o.RenderSvg != null)
                    {
                        GraphicsOutput.RenderSvg(db.GetCoordinateSpace(), db, db, o.RenderSvg);
                    }
                });
        }

        private static void InitializeDatabaseFrom(string dbPath, string scenarioJsonFilename)
        {
            var scenario = ScenarioLoader.LoadScenario(scenarioJsonFilename);

            using (var db = SqliteSimDatabase.New(dbPath, scenario.coordinateSpace))
            {
                db.AddSegments(scenario.networkDatabase.EnumerateSegments());
                db.AddSegmentLinks(scenario.networkDatabase.EnumerateSegmentLinks());
                db.AddStations(scenario.networkDatabase.EnumerateStations());

                // This is super weird. GetQuadTree shouldn't be normally exposed. At the same time, we do not want
                // to do duplicate work. Possible solutions:
                //  - do not expose the methods on the interface, but do so on the implementation (but then we're bound to a specific impl)
                //  - move the processing from GeoJsonDatabase's constructor to NetworkImportUtility so that code
                //    doesn't have to be repeated
                db.PutQuadTree(((GeoJsonNetworkDatabase)scenario.networkDatabase).GetQuadTree());

                db.AddUnits(scenario.units);
            }
        }
    }
}
