using System;
using System.Threading;

namespace GenericBuffer.Core
{
    public class IncrementalClock
    {
        private long _tick_;
        private readonly long _step_;

        public long CurrentTick => _tick_;
        public DateTime CurrentDateTime => new DateTime(_tick_);

        public IncrementalClock(DateTime startDate) : this(startDate.Ticks) { }
        public IncrementalClock(DateTime startDate, TimeSpan step) : this(startDate.Ticks, step.Ticks) { }

        public IncrementalClock(long startTick = 0, long step = 1)
        {
            _tick_ = startTick;
            _step_ = step;
        }

        public DateTime GetNextDateTime() => new DateTime(Interlocked.Add(ref _tick_, _step_));
    }
}
