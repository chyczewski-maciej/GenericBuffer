using System;
using System.Threading;
using System.Threading.Tasks;

namespace GenericBuffer.Core
{
    public class AsyncGenericBuffer<T> : IAsyncGenericBuffer<T>
    {
        private readonly Func<Task<T>> _factory_;
        private readonly Func<DateTime> _clock_;
        private readonly TimeSpan _bufferingPeriod_;
        
        private readonly SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1);
        private DateTime validUntil = DateTime.MinValue;
        private T buffer;

        public AsyncGenericBuffer(Func<Task<T>> factory, TimeSpan bufferingPeriod) : this(factory, bufferingPeriod, () => DateTime.Now) { }

        public AsyncGenericBuffer(Func<Task<T>> factory, TimeSpan bufferingPeriod, Func<DateTime> clock)
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
            await semaphoreSlim.WaitAsync();
            try
            {
                buffer = await _factory_();
                validUntil = NewValidUntil();
                return buffer;
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        public async Task<T> GetValueAsync()
        {
            if (_clock_() < validUntil)
                return buffer;

            await semaphoreSlim.WaitAsync();
            try
            {
                if (_clock_() < validUntil)
                    return buffer;

                validUntil = NewValidUntil();
                buffer = await _factory_();
            }
            finally
            {
                semaphoreSlim.Release();
            }

            return buffer;
        }

        private DateTime NewValidUntil() => _clock_().Add(_bufferingPeriod_);
    }
}