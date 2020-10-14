using System;
using System.Threading;
using System.Threading.Tasks;

namespace GenericBuffer.Core
{
    public class AsyncGenericBuffer<T> : IAsyncGenericBuffer<T>
    {
        private readonly SemaphoreSlim _semaphoreSlim_ = new SemaphoreSlim(1);
        private readonly Func<T, CancellationToken, Task<T>> _factory_;
        private readonly Func<DateTime> _clock_;
        private readonly TimeSpan _bufferingPeriod_;

        private DateTime validUntil = DateTime.MinValue;
        private T buffer;

        public AsyncGenericBuffer(Func<T, CancellationToken, Task<T>> factory, T initialValue, TimeSpan bufferingPeriod) : this(factory, initialValue, bufferingPeriod, () => DateTime.UtcNow) { }

        public AsyncGenericBuffer(Func<T, CancellationToken, Task<T>> factory, T initialValue, TimeSpan bufferingPeriod, Func<DateTime> clock)
        {
            _factory_ = factory ?? throw new ArgumentNullException(nameof(factory));
            _clock_ = clock ?? throw new ArgumentNullException(nameof(clock));
            _bufferingPeriod_ = bufferingPeriod;
            buffer = initialValue;
        }

        public AsyncGenericBuffer(Func<CancellationToken, Task<T>> factory, TimeSpan bufferingPeriod) : this(factory, bufferingPeriod, () => DateTime.UtcNow) { }
        public AsyncGenericBuffer(Func<CancellationToken, Task<T>> factory, TimeSpan bufferingPeriod, Func<DateTime> clock) : this(factory, default, bufferingPeriod, clock) { }

        public AsyncGenericBuffer(Func<CancellationToken, Task<T>> factory, T initialValue, TimeSpan bufferingPeriod, Func<DateTime> clock)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));
            _factory_ = (_, ct) => factory(ct);
            _clock_ = clock ?? throw new ArgumentNullException(nameof(clock)); ;
            _bufferingPeriod_ = bufferingPeriod;
            buffer = initialValue;
        }

        public async Task ResetAsync(CancellationToken cancellationToken)
        {
            await _semaphoreSlim_.WaitAsync(cancellationToken);
            try
            {
                buffer = default;
                validUntil = DateTime.MinValue;
            }
            finally
            {
                _semaphoreSlim_.Release();
            }
        }

        public async Task<T> ForceRefreshAsync(CancellationToken cancellationToken)
        {
            return await RefreshAsync(false, cancellationToken);
        }

        public async Task<T> GetValueAsync(CancellationToken cancellationToken)
        {
            if (_clock_() < validUntil)
                return buffer;

            return await RefreshAsync(true, cancellationToken);
        }

        private DateTime NewValidUntil() => _clock_().Add(_bufferingPeriod_);

        private async Task<T> RefreshAsync(Boolean considerBuffer, CancellationToken cancellationToken)
        {
            await _semaphoreSlim_.WaitAsync(cancellationToken);
            try
            {
                if (considerBuffer && _clock_() < validUntil)
                    return buffer;

                buffer = await _factory_(buffer, cancellationToken);
                validUntil = NewValidUntil();
                return buffer;
            }
            finally
            {
                _semaphoreSlim_.Release();
            }
        }
    }
}