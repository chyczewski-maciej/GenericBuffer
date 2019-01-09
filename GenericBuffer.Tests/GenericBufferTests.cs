using GenericBuffer.Core;
using System;
using Xunit;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Runtime.CompilerServices;

namespace GenericBuffer.Tests
{
    public class GenericBufferTests
    {
        [Fact]
        public void Throws_ArgumentNullException_when_factory_function_is_null()
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
                ClockFactory.FrozenClock()));

            Assert.Throws<ArgumentNullException>(() => new GenericBuffer<object>(
                factory: funcT,
                bufferingPeriod: TimeSpan.Zero));

            Assert.Throws<ArgumentNullException>(() => new GenericBuffer<object>(
                factory: funcT,
                bufferingPeriod: TimeSpan.Zero,
                ClockFactory.FrozenClock()));
            // ReSharper restore ExpressionIsAlwaysNull
        }

        [Fact]
        public void Throws_ArgumentNullException_when_clock_func_is_null()
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
        public void Returns_new_value_after_buffering_period_passes()
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
        public void Returns_the_same_object_while_in_the_same_buffering_period()
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
        public void Throws_the_same_exception_as_factory()
        {
            var expectedException = new Exception();
            var genericBuffer = new GenericBuffer<object>(
                factory: () => throw expectedException,
                bufferingPeriod: TimeSpan.Zero, clock:
                ClockFactory.FrozenClock());

            Exception actualException = Record.Exception(() => genericBuffer.GetValue());

            Assert.Same(expectedException, actualException);
        }

        [Fact]
        public void Reset_forces_creating_a_new_value()
        {
            var genericBuffer = new GenericBuffer<object>(
                factory: () => new object(),
                bufferingPeriod: TimeSpan.FromTicks(1),
                clock: ClockFactory.FrozenClock());

            var firstObject = genericBuffer.GetValue();
            Assert.Same(firstObject, genericBuffer.GetValue());
            Assert.Same(firstObject, genericBuffer.GetValue());

            genericBuffer.Reset();

            Assert.NotSame(firstObject, genericBuffer.GetValue());
        }

        [Fact]
        public void Force_refresh_creates_new_value_even_if_the_old_one_is_still_valid()
        {
            var genericBuffer = new GenericBuffer<object>(
                factory: () => new object(),
                bufferingPeriod: TimeSpan.FromTicks(1),
                clock: ClockFactory.FrozenClock());


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
        public async Task Creates_item_only_once_when_get_value_is_called_in_parallel()
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
                clock: ClockFactory.FrozenClock());

            var tasks = Enumerable.Range(0, 100)
                        .Select(_ => Task.Run(() => genericBuffer.GetValue()));

            blockFactoryMethod = false;
            await Task.Delay(200);
            object[] results = await Task.WhenAll(tasks);

            foreach (var result in results)
                Assert.Same(results.First(), result);
        }

        [Fact]
        public async Task Gets_GC_collected()
        {
            // GC does not clean up memory allocated in same method
            // https://github.com/dotnet/coreclr/issues/20156
            WeakReference weakReference = CreateBufferWeakReference();

            // Force GC to collect objects
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.False(weakReference.IsAlive);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        WeakReference CreateBufferWeakReference()
        {
            var buffer = new GenericBuffer<Object>(() => new Object(), TimeSpan.FromSeconds(5));
            return new WeakReference(buffer);
        }
    }
}