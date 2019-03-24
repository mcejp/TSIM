// https://docs.microsoft.com/en-us/visualstudio/test/unit-test-basics?view=vs-2017

using System.Numerics;
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

        [Fact]
        public void Test2()
        {
            var dir = new Vector3(10, 10, 0);
            var quat = Utility.DirectionVectorToQuaternion(dir);
            var rot = Vector3.Transform(new Vector3(1, 0, 0), quat);
            Assert.Equal(0.707, rot.X, 3);
            Assert.Equal(0.707, rot.Y, 3);
            Assert.Equal(0, rot.Z, 3);
        }
    }
}
