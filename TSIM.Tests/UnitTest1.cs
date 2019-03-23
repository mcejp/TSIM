// https://docs.microsoft.com/en-us/visualstudio/test/unit-test-basics?view=vs-2017

using TSIM.Model;
using Xunit;

namespace TSIM.Tests
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            var space = new SimulationCoordinateSpace(50.083239, 14.435278);
            var vector = space.To(50.1, 14.5);
            Assert.Equal(4617.97265625, vector.X, 5);
            Assert.Equal(1863.73815918, vector.Y, 5);
        }
    }
}
