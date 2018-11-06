using GenericBuffer.Core;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace GenericBuffer.Tests
{
    public class AsyncGenericBufferTests
    {
        [Fact]
        public void ThrowsArgumentNullExceptionWhenFactoryFunctionIsNull()
        {
            Func<object, Task<object>> funcTT = null;
            Func<Task<object>> funcT = null;

            Assert.Throws<ArgumentNullException>(() => new AsyncGenericBuffer<object>(
                factory: funcTT,
                initialValue: Task.FromResult(new object()),
                bufferingPeriod: TimeSpan.Zero));

            Assert.Throws<ArgumentNullException>(() => new AsyncGenericBuffer<object>(
                factory: funcTT,
                initialValue: new object(),
                bufferingPeriod: TimeSpan.Zero,
                ClockFactory.UtcClock()));

            Assert.Throws<ArgumentNullException>(() => new AsyncGenericBuffer<object>(
                factory: funcT,
                bufferingPeriod: TimeSpan.Zero));

            Assert.Throws<ArgumentNullException>(() => new AsyncGenericBuffer<object>(
                factory: funcT,
                bufferingPeriod: TimeSpan.Zero,
                ClockFactory.UtcClock()));
        }

        [Fact]
        public void ThrowsArgumentNullExceptionWhenClockFuncIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new AsyncGenericBuffer<object>(
                factory: _ => Task.FromResult(new object()),
                initialValue: new object(),
                bufferingPeriod: TimeSpan.Zero,
                clock: null));

            Assert.Throws<ArgumentNullException>(() => new AsyncGenericBuffer<object>(
                factory: () => Task.FromResult(new object()),
                bufferingPeriod: TimeSpan.Zero,
                clock: null));
        }

        [Fact]
        public async Task ReturnsNewValueAfterBufferingPeriodPasses()
        {
            DateTime dateTime = DateTime.MinValue;
            var bufferingPeriod = TimeSpan.FromTicks(1);
            var incrementalClock = new IncrementalClock();
            int executions = 0;
            var AsyncGenericBuffer = new AsyncGenericBuffer<int>(factory: () => Task.FromResult(executions++), bufferingPeriod: bufferingPeriod, clock: () => dateTime);


            Assert.Equal(0, await AsyncGenericBuffer.GetValueAsync()); // Before buffering period is past

            dateTime += bufferingPeriod;
            Assert.Equal(1, await AsyncGenericBuffer.GetValueAsync()); // After first buffering period is past

            dateTime += bufferingPeriod;
            Assert.Equal(2, await AsyncGenericBuffer.GetValueAsync()); // After second buffering period is past
        }

        [Fact]
        public async Task ReturnsTheSameObjectWhileInTheSameBufferingPeriod()
        {
            DateTime dateTime = DateTime.MinValue;
            var bufferingPeriod = TimeSpan.FromTicks(1);
            var AsyncGenericBuffer = new AsyncGenericBuffer<object>(factory: () => Task.FromResult(new object()), bufferingPeriod: bufferingPeriod, clock: () => dateTime);

            object firstValue = await AsyncGenericBuffer.GetValueAsync();
            Assert.Same(firstValue, await AsyncGenericBuffer.GetValueAsync());
            Assert.Same(firstValue, await AsyncGenericBuffer.GetValueAsync());

            dateTime += bufferingPeriod;
            Assert.NotSame(firstValue, await AsyncGenericBuffer.GetValueAsync());
            Assert.Same(await AsyncGenericBuffer.GetValueAsync(), await AsyncGenericBuffer.GetValueAsync());
        }

        [Fact]
        public async Task ThrowsTheSameExceptionAsFactory()
        {
            var expectedException = new Exception();
            var AsyncGenericBuffer = new AsyncGenericBuffer<object>(
                factory: () => throw expectedException,
                bufferingPeriod: TimeSpan.Zero,
                clock: ClockFactory.UtcClock());

            Exception recoredException = await Record.ExceptionAsync(async () => await AsyncGenericBuffer.GetValueAsync());

            Assert.Same(expectedException, recoredException);
        }

        [Fact]
        public async Task ResetForcesCreatingANewValue()
        {
            var AsyncGenericBuffer = new AsyncGenericBuffer<object>(
                factory: () => Task.FromResult(new object()),
                bufferingPeriod: TimeSpan.FromTicks(1),
                clock: ClockFactory.FrozenClock(DateTime.MinValue));

            var firstObject = await AsyncGenericBuffer.GetValueAsync();
            Assert.Same(firstObject, await AsyncGenericBuffer.GetValueAsync());
            Assert.Same(firstObject, await AsyncGenericBuffer.GetValueAsync());

            await AsyncGenericBuffer.ResetAsync();

            Assert.NotSame(firstObject, await AsyncGenericBuffer.GetValueAsync());
        }

        [Fact]
        public async Task ForceRefreshCreatesNewValueEvenIfTheOldOneIsStillValid()
        {
            var asyncGenericBuffer = new AsyncGenericBuffer<object>(
                factory: () => Task.FromResult(new object()),
                bufferingPeriod: TimeSpan.FromTicks(1),
                clock: ClockFactory.FrozenClock(DateTime.MinValue));

            var val1 = await asyncGenericBuffer.GetValueAsync();
            var val2 = await asyncGenericBuffer.GetValueAsync();
            var forced = await asyncGenericBuffer.ForceRefreshAsync();
            var val3 = await asyncGenericBuffer.GetValueAsync();
            var val4 = await asyncGenericBuffer.GetValueAsync();

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

            var asyncGenericBuffer = new AsyncGenericBuffer<object>(
                factory: () =>
                {
                    while (blockFactoryMethod) { }
                    return Task.FromResult(new object());
                },
                bufferingPeriod: TimeSpan.FromTicks(1),
                clock: ClockFactory.FrozenClock(DateTime.MinValue));

            var tasks = Enumerable.Range(0, 1000)
                        .Select(_ => Task.Run(async () => await asyncGenericBuffer.GetValueAsync()));

            blockFactoryMethod = false;
            await Task.Delay(2000);
            object[] results = await Task.WhenAll(tasks);

            foreach (var result in results)
                Assert.Same(results.First(), result);
        }
    }
}