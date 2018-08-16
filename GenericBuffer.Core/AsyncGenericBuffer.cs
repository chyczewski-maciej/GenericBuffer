using System;
using System.Threading;
using System.Threading.Tasks;

namespace GenericBuffer.Core
{
    public class AsyncGenericBuffer<T> : IAsyncGenericBuffer<T>
    {
        private readonly Func<T, Task<T>> _factory_;
        private readonly Func<DateTime> _clock_;
        private readonly TimeSpan _bufferingPeriod_;

        private readonly SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1);
        private DateTime validUntil = DateTime.MinValue;
        private T buffer;

        public AsyncGenericBuffer(Func<Task<T>> factory, TimeSpan bufferingPeriod) : this(factory, bufferingPeriod, () => DateTime.Now) { }
        public AsyncGenericBuffer(Func<Task<T>> factory, TimeSpan bufferingPeriod, Func<DateTime> clock) : this(_ => factory(), bufferingPeriod, clock) { }
        public AsyncGenericBuffer(Func<T, Task<T>> factory, TimeSpan bufferingPeriod) : this(factory, bufferingPeriod, () => DateTime.Now) { }

        public AsyncGenericBuffer(Func<T, Task<T>> factory, TimeSpan bufferingPeriod, Func<DateTime> clock)
        {
            _factory_ = factory ?? throw new ArgumentNullException(nameof(factory));
            _bufferingPeriod_ = bufferingPeriod;
            _clock_ = clock;
        }

        public async Task ResetAsync()
        {
            await semaphoreSlim.WaitAsync();
            try
            {
                buffer = default;
                validUntil = DateTime.MinValue;
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        public async Task<T> ForceRefreshAsync()
        {
            return await RefreshAsync(considerBuffer: false);
        }

        public async Task<T> GetValueAsync()
        {
            if (_clock_() < validUntil)
                return buffer;

            return await RefreshAsync(considerBuffer: true);
        }

        private DateTime NewValidUntil() => _clock_().Add(_bufferingPeriod_);

        private async Task<T> RefreshAsync(bool considerBuffer)
        {


            await semaphoreSlim.WaitAsync();
            try
            {
                if (considerBuffer && _clock_() < validUntil)
                    return buffer;

                buffer = await _factory_(buffer);
                validUntil = NewValidUntil();
                return buffer;
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }
    }
}