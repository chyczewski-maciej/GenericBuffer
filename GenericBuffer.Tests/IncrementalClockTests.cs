using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace GenericBuffer.Tests
{
    public class IncrementalClockTests
    {
        private Random random = new Random();

        private Task RandomDelay() => Task.Delay(TimeSpan.FromTicks(random.Next(1000)));

        [Fact]
        public async Task GetNextDateTimeMustReturnIncrementalTicks()
        {
            long startTick = 0;
            long expectedTick = 0;
            var incrementalClock = new IncrementalClock(startTick);
            Func<DateTime> clock = incrementalClock.GetNextDateTime;

            while (expectedTick <= 5)
            {
                await RandomDelay(); // Make sure it's not real time dependent
                Assert.Equal(++expectedTick, clock().Ticks);
                Assert.Equal(expectedTick, incrementalClock.CurrentTick);
            }
        }

        [Fact]
        public async Task GetNextDateTimeMustReturnIncrementTicksInMultiThreadContext()
        {
            long startTick = 56749874;
            var incrementalClock = new IncrementalClock(startTick);
            IEnumerable<long> range = Enumerable.Range(1, 5000).Select(x => (long)x);

            long[] ticksFromincrementalClock = await Task.WhenAll(range
                .Select(async _ =>
                    {
                        await RandomDelay();
                        return incrementalClock.GetNextDateTime().Ticks;
                    }));

            List<long> expectedTicks = range.Select(x => x + startTick).ToList();
            Assert.Equal(ticksFromincrementalClock.OrderBy(x => x).ToList(), expectedTicks);
        }
    }
}
