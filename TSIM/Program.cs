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

            // 3. render 2D/3D view
            GraphicsOutput.RenderSvg(db.GetCoordinateSpace(), db, db.EnumerateUnits(), "output.svg");
        }

        private static void InitializeDatabaseFrom(string dbPath, string scenarioJsonFilename)
        {
            var scenario = ScenarioLoader.LoadScenario(scenarioJsonFilename);

            using (var db = SqliteSimDatabase.New(dbPath, scenario.coordinateSpace))
            {
                db.AddSegments(scenario.networkDatabase.EnumerateSegments());
                db.AddUnits(scenario.units);
            }
        }
    }
}
