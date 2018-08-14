using GenericBuffer.Core;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace GenericBuffer.Tests
{
    public class FakeClockTests
    {
        private Random random = new Random();

        [Fact]
        public async Task GetNextDateTimeMustReturnIncrementalTicks()
        {
            long startTick = 0;
            var fakeClock = new FakeClock(startTick);

            for (long currentTick = 1; currentTick < 10; currentTick++)
            {
                Assert.Equal(currentTick, fakeClock.GetNextDateTime().Ticks);
                await Task.Delay(random.Next(100)); // Make sure it's not real time dependent
            }
        }

        [Fact]
        public void GetNextDateTimeMustReturnIncrementTicksInMultiThreadContext()
        {
            long startTick = 56749874;
            var fakeClock = new FakeClock(startTick);

            System.Collections.Generic.List<long> ticksFromFakeClock = Enumerable.Range(1, 5000).AsParallel().Select(_ => fakeClock.GetNextDateTime().Ticks).OrderBy(x => x).ToList();
            Assert.True(ticksFromFakeClock.SequenceEqual(Enumerable.Range(1, 5000).Select(x => (long)x + startTick)));
        }
    }
}
