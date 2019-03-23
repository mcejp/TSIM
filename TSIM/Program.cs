using System;
using TSIM.Model;
using TSIM.RailroadDatabase;

namespace TSIM
{
    static class Program
    {
        static void Main(string[] args)
        {
            // 1. load scenario
            var scenario = ScenarioLoader.LoadScenario("data/scenario.json");

            // 2. simulate

            // 3. render 2D/3D view
            GraphicsOutput.RenderSvg(scenario.coordinateSpace, scenario.networkDatabase, scenario.units, "output.svg");
        }
    }
}
