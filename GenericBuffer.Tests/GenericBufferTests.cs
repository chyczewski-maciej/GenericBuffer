using GenericBuffer.Core;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace GenericBuffer.Tests
{
    public class GenericBufferTests
    {
        Func<DateTime> CreateFakeClock()
        {
            var enumerator = DateTimeSequence().GetEnumerator();

            return () =>
            {
                enumerator.MoveNext();
                return enumerator.Current;
            };


            IEnumerable<DateTime> DateTimeSequence()
            {
                long tick = 0;
                while (tick < long.MaxValue)
                    yield return new DateTime(Interlocked.Increment(ref tick));
            }
        }


        [Fact]
        public void Test()
        {
            int executions = 0;
            Func<int> factory = () => executions++;
            var fakeClock = new FakeClock();

            var genericBuffer = new GenericBuffer<int>(factory, TimeSpan.FromMilliseconds(5), fakeClock.GetNextDateTime);

        }
    }
}
