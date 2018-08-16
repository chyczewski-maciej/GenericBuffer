using GenericBuffer.Core;
using System;
using System.Threading.Tasks;
using Xunit;

namespace GenericBuffer.Tests
{
    public class AsyncGenericBufferTests
    {
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
            var AsyncGenericBuffer = new AsyncGenericBuffer<object>(factory: () => throw expectedException, bufferingPeriod: TimeSpan.Zero, clock: ClockFactory.UtcClock());

            Exception recoredException = await Record.ExceptionAsync(async () => await AsyncGenericBuffer.GetValueAsync());

            Assert.Same(expectedException, recoredException);
        }

        [Fact]
        public async Task ResetForcesCreatingANewValue()
        {
            var AsyncGenericBuffer = new AsyncGenericBuffer<object>(factory: () => Task.FromResult(new object()), bufferingPeriod: TimeSpan.FromTicks(1), clock: ClockFactory.FrozenClock(DateTime.MinValue));

            var firstObject = await AsyncGenericBuffer.GetValueAsync();
            Assert.Same(firstObject, await AsyncGenericBuffer.GetValueAsync());
            Assert.Same(firstObject, await AsyncGenericBuffer.GetValueAsync());

            await AsyncGenericBuffer.ResetAsync();

            Assert.NotSame(firstObject, await AsyncGenericBuffer.GetValueAsync());
        }
    }
}