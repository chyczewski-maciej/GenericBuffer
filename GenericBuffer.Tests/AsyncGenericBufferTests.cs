using GenericBuffer.Core;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

namespace GenericBuffer.Tests
{
    public class AsyncGenericBufferTests
    {
        [Fact]
        public void Throws_ArgumentNullException_when_factory_function_is_null()
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
                ClockFactory.FrozenClock()));

            Assert.Throws<ArgumentNullException>(() => new AsyncGenericBuffer<object>(
                factory: funcT,
                bufferingPeriod: TimeSpan.Zero));

            Assert.Throws<ArgumentNullException>(() => new AsyncGenericBuffer<object>(
                factory: funcT,
                bufferingPeriod: TimeSpan.Zero,
                ClockFactory.FrozenClock()));
        }

        [Fact]
        public void Throws_ArgumentNullException_when_clock_func_is_null()
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
        public async Task Returns_new_value_after_buffering_period_passes()
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
        public async Task Returns_the_same_object_while_in_the_same_buffering_period()
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
        public async Task Throws_the_same_exception_as_factory()
        {
            var expectedException = new Exception();
            var AsyncGenericBuffer = new AsyncGenericBuffer<object>(
                factory: () => throw expectedException,
                bufferingPeriod: TimeSpan.Zero,
                clock: ClockFactory.FrozenClock());

            Exception recoredException = await Record.ExceptionAsync(async () => await AsyncGenericBuffer.GetValueAsync());

            Assert.Same(expectedException, recoredException);
        }

        [Fact]
        public async Task Reset_forces_creating_a_new_value()
        {
            var AsyncGenericBuffer = new AsyncGenericBuffer<object>(
                factory: () => Task.FromResult(new object()),
                bufferingPeriod: TimeSpan.FromTicks(1),
                clock: ClockFactory.FrozenClock());

            var firstObject = await AsyncGenericBuffer.GetValueAsync();
            Assert.Same(firstObject, await AsyncGenericBuffer.GetValueAsync());
            Assert.Same(firstObject, await AsyncGenericBuffer.GetValueAsync());

            await AsyncGenericBuffer.ResetAsync();

            Assert.NotSame(firstObject, await AsyncGenericBuffer.GetValueAsync());
        }

        [Fact]
        public async Task Force_fefresh_creates_new_value_even_if_the_old_one_is_still_valid()
        {
            var asyncGenericBuffer = new AsyncGenericBuffer<object>(
                factory: () => Task.FromResult(new object()),
                bufferingPeriod: TimeSpan.FromTicks(1),
                clock: ClockFactory.FrozenClock());

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
        public async Task Creates_item_only_once_when_get_value_is_called_in_parallel()
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
                clock: ClockFactory.FrozenClock());

            var tasks = Enumerable.Range(0, 1000)
                        .Select(_ => Task.Run(async () => await asyncGenericBuffer.GetValueAsync()));

            blockFactoryMethod = false;
            await Task.Delay(2000);
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
            var buffer = new AsyncGenericBuffer<Object>(() => Task.FromResult(new Object()), TimeSpan.FromSeconds(5));
            return new WeakReference(buffer);
        }
    }
}