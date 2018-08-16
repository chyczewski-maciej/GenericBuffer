using System;
using System.Threading;

namespace GenericBuffer.Tests
{
    public class ClockFactory
    {
        public static Func<DateTime> FrozenClock(DateTime dateTime) => () => dateTime;
        public static Func<DateTime> UtcClock() => () => DateTime.UtcNow;
        public static Func<DateTime> UtcClock(DateTime startDateTime) => UtcClockWithOffset(startDateTime.Subtract(DateTime.UtcNow));
        public static Func<DateTime> UtcClockWithOffset(TimeSpan offset) => () => DateTime.UtcNow.Add(offset);
        public static Func<DateTime> IncrementalClock() => new IncrementalClock().GetNextDateTime;

        public static Func<DateTime> EnumerableClock(DateTime[] dateTimes, bool loop = true)
        {
            if (dateTimes == null)
                throw new ArgumentNullException(nameof(dateTimes));

            if (dateTimes.Length == 0)
                throw new ArgumentException(nameof(dateTimes) + " cannot be empty");

            int index = 0;
            object locker = new object();

            return () =>
            {
                lock (locker)
                {
                    if (index >= dateTimes.Length)
                        if (loop)
                            index = 0;
                        else
                            throw new IndexOutOfRangeException(nameof(EnumerableClock) + " has exceeded returned all possible values");

                    return dateTimes[index++];
                }
            };
        }
    }

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
