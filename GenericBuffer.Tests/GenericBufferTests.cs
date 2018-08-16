using GenericBuffer.Core;
using System;
using Xunit;

namespace GenericBuffer.Tests
{
    public class GenericBufferTests
    {
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
            var genericBuffer = new GenericBuffer<object>(factory: () => throw expectedException, bufferingPeriod: TimeSpan.Zero, clock: ClockFactory.UtcClock());

            var recoredException = Record.Exception(() => genericBuffer.GetValue());

            Assert.Same(expectedException, recoredException);
        }

        [Fact]
        public void ResetForcesCreatingANewValue()
        {
            var genericBuffer = new GenericBuffer<object>(factory: () => new object(), bufferingPeriod: TimeSpan.FromTicks(1), clock: ClockFactory.FrozenClock(DateTime.MinValue));

            var firstObject = genericBuffer.GetValue();
            Assert.Same(firstObject, genericBuffer.GetValue());
            Assert.Same(firstObject, genericBuffer.GetValue());

            genericBuffer.Reset();

            Assert.NotSame(firstObject, genericBuffer.GetValue());
        }
    }
}