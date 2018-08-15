using System;

namespace GenericBuffer.Core
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
}
