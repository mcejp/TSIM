using TSIM.RailroadDatabase;

namespace TSIM
{
    static class Program
    {
        static void Main(string[] args)
        {
            string dbPath = "simdb.sqlite";

            // 0. import GeoJSON data to SQLite
            InitializeDatabaseFrom(dbPath, "data/scenario.json");

            // 1. open database
            var db = SqliteSimDatabase.Open(dbPath);

            // 2. simulate
            var sim = new Simulation(db.GetCoordinateSpace(), db, db);
            sim.Units.SetUnitSpeed(0, 50 / 3.6f);

            for (var i = 0; i < 50; i++)
            {
                sim.Step(1.0f);
            }

            // 3. render 2D/3D view
            GraphicsOutput.RenderSvg(db.GetCoordinateSpace(), db, db, "output.svg");
        }

        private static void InitializeDatabaseFrom(string dbPath, string scenarioJsonFilename)
        {
            var scenario = ScenarioLoader.LoadScenario(scenarioJsonFilename);

            using (var db = SqliteSimDatabase.New(dbPath, scenario.coordinateSpace))
            {
                db.AddSegments(scenario.networkDatabase.EnumerateSegments());
                db.AddSegmentLinks(scenario.networkDatabase.EnumerateSegmentLinks());
                db.AddUnits(scenario.units);
            }
        }
    }
}
