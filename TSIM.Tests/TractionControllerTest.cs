// https://docs.microsoft.com/en-us/visualstudio/test/unit-test-basics?view=vs-2017

using System;
using TSIM;
using Xunit;

namespace TSIM.Tests
{
    public class TrainModelTest
    {
        [Fact]
        public void TestTrainModel1() {
            var (a, v1, mode) = TrainModel.AccelerationToFullyStopAfter2(
                    v: 0.46089262f, distToGoal: 0.08170131f, accelMax: 1.0f, decelNom: 1.3f, maxVelocity: 80.0f / 3.6f, dt: 0.1f);
            Assert.Equal(0.46089414, v1, 6);
            Assert.Equal(-1.3f, a, 3);
        }

        [Fact]
        public void TestTrainModel2() {
            var (a, v1, mode) = TrainModel.AccelerationToFullyStopAfter2(
                    v: 0.46089262f, distToGoal: 0.08170131f, accelMax: 1.0f, decelNom: 1.3f, maxVelocity: 80.0f / 3.6f, dt: 0.2f);
            Assert.Equal(-1.414f, a, 3);
        }
    }
}
