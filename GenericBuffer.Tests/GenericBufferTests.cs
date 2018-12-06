using GenericBuffer.Core;
using System;
using Xunit;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace GenericBuffer.Tests
{
    public class GenericBufferTests
    {
        [Fact]
        public void ThrowsArgumentNullExceptionWhenFactoryFunctionIsNull()
        {
            Func<object, object> funcTT = null;
            Func<object> funcT = null;

            // ReSharper disable ExpressionIsAlwaysNull
            Assert.Throws<ArgumentNullException>(() => new GenericBuffer<object>(
                factory: funcTT,
                initialValue: new object(),
                bufferingPeriod: TimeSpan.Zero));

            Assert.Throws<ArgumentNullException>(() => new GenericBuffer<object>(
                factory: funcTT,
                initialValue: new object(),
                bufferingPeriod: TimeSpan.Zero,
                ClockFactory.UtcClock()));

            Assert.Throws<ArgumentNullException>(() => new GenericBuffer<object>(
                factory: funcT,
                bufferingPeriod: TimeSpan.Zero));

            Assert.Throws<ArgumentNullException>(() => new GenericBuffer<object>(
                factory: funcT,
                bufferingPeriod: TimeSpan.Zero,
                ClockFactory.UtcClock()));
            // ReSharper restore ExpressionIsAlwaysNull
        }

        [Fact]
        public void ThrowsArgumentNullExceptionWhenClockFuncIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new GenericBuffer<object>(
                factory: _ => new object(),
                initialValue: new object(),
                bufferingPeriod: TimeSpan.Zero,
                clock: null));

            Assert.Throws<ArgumentNullException>(() => new GenericBuffer<object>(
                factory: () => new object(),
                bufferingPeriod: TimeSpan.Zero,
                clock: null));
        }

        [Fact]
        public void ReturnsNewValueAfterBufferingPeriodPasses()
        {
            DateTime dateTime = DateTime.MinValue;
            var bufferingPeriod = TimeSpan.FromTicks(1);
            var incrementalClock = new IncrementalClock();
            int executions = 0;
            var genericBuffer = new GenericBuffer<int>(factory: () => executions++, bufferingPeriod: bufferingPeriod, clock: () => dateTime);


            Assert.Equal(0, genericBuffer.GetValue()); // Before buffering period is past

            dateTime += bufferingPeriod;
            Assert.Equal(1, genericBuffer.GetValue()); // After first buffering period is past

            dateTime += bufferingPeriod;
            Assert.Equal(2, genericBuffer.GetValue()); // After second buffering period is past
        }

        [Fact]
        public void ReturnsTheSameObjectWhileInTheSameBufferingPeriod()
        {
            DateTime dateTime = DateTime.MinValue;
            var bufferingPeriod = TimeSpan.FromTicks(1);
            var genericBuffer = new GenericBuffer<object>(factory: () => new object(), bufferingPeriod: bufferingPeriod, clock: () => dateTime);

            object firstValue = genericBuffer.GetValue();
            Assert.Same(firstValue, genericBuffer.GetValue());
            Assert.Same(firstValue, genericBuffer.GetValue());

            dateTime += bufferingPeriod;
            Assert.NotSame(firstValue, genericBuffer.GetValue());
            Assert.Same(genericBuffer.GetValue(), genericBuffer.GetValue());
        }

        [Fact]
        public void ThrowsTheSameExceptionAsFactory()
        {
            var expectedException = new Exception();
            var genericBuffer = new GenericBuffer<object>(
                factory: () => throw expectedException,
                bufferingPeriod: TimeSpan.Zero, clock:
                ClockFactory.UtcClock());

            Exception actualException = Record.Exception(() => genericBuffer.GetValue());

            Assert.Same(expectedException, actualException);
        }

        [Fact]
        public void ResetForcesCreatingANewValue()
        {
            var genericBuffer = new GenericBuffer<object>(
                factory: () => new object(),
                bufferingPeriod: TimeSpan.FromTicks(1),
                clock: ClockFactory.FrozenClock(DateTime.MinValue));

            var firstObject = genericBuffer.GetValue();
            Assert.Same(firstObject, genericBuffer.GetValue());
            Assert.Same(firstObject, genericBuffer.GetValue());

            genericBuffer.Reset();

            Assert.NotSame(firstObject, genericBuffer.GetValue());
        }

        [Fact]
        public void ForceRefreshCreatesNewValueEvenIfTheOldOneIsStillValid()
        {
            var genericBuffer = new GenericBuffer<object>(
                factory: () => new object(),
                bufferingPeriod: TimeSpan.FromTicks(1),
                clock: ClockFactory.FrozenClock(DateTime.MinValue));


            var val1 = genericBuffer.GetValue();
            var val2 = genericBuffer.GetValue();
            var forced = genericBuffer.ForceRefresh();
            var val3 = genericBuffer.GetValue();
            var val4 = genericBuffer.GetValue();

            Assert.Same(val1, val2);
            Assert.NotSame(val1, forced);
            Assert.Same(forced, val3);
            Assert.Same(val3, val4);
        }

        [Fact]
        public async Task CreatesItemOnlyOnceWhenGetValueIsCalledInParallel()
        {
            var rand = new Random();
            var blockFactoryMethod = true;

            var genericBuffer = new GenericBuffer<object>(
                factory: () =>
                {
                    while (blockFactoryMethod) { }
                    return new object();
                },
                bufferingPeriod: TimeSpan.FromTicks(1),
                clock: ClockFactory.FrozenClock(DateTime.MinValue));

            var tasks = Enumerable.Range(0, 100)
                        .Select(_ => Task.Run(() => genericBuffer.GetValue()));

            blockFactoryMethod = false;
            await Task.Delay(200);
            object[] results = await Task.WhenAll(tasks);

            foreach (var result in results)
                Assert.Same(results.First(), result);
        }
    }
}