using System;
using System.Threading;

namespace GenericBuffer.Core
{
    public class FakeClock
    {
        private long tick = 0;
        long CurrentTick => tick;

        public FakeClock(DateTime startDate) : this(startDate.Ticks) { }

        public FakeClock(long startTick = 0)
        {
            tick = startTick;
        }

        public DateTime GetNextDateTime() => new DateTime(Interlocked.Increment(ref tick));
    }
}
