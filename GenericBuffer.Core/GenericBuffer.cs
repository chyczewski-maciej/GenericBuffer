using System;

namespace GenericBuffer.Core
{
    public class GenericBuffer<T> : IGenericBuffer<T>
    {
        private readonly Func<T> _factory_;
        private readonly TimeSpan _bufferingPeriod_;
        private readonly Func<DateTime> _clock_;

        private readonly object _locker_ = new object();

        private DateTime validUntil = DateTime.MinValue;
        private T buffer;

        public GenericBuffer(Func<T> factory, TimeSpan bufferingPeriod) : this(factory, bufferingPeriod, () => DateTime.Now) { }

        public GenericBuffer(Func<T> factory, TimeSpan bufferingPeriod, Func<DateTime> clock)
        {
            _factory_ = factory ?? throw new ArgumentNullException(nameof(factory));
            _bufferingPeriod_ = bufferingPeriod;
            _clock_ = clock;
        }

        public void Reset()
        {
            lock (_locker_)
            {
                buffer = default;
                validUntil = DateTime.MinValue;
            }
        }

        public T ForceRefresh()
        {
            lock (_locker_)
            {
                buffer = _factory_();
                validUntil = NewValidUntil();
            }

            return buffer;
        }

        public T GetValue()
        {
            if (_clock_() < validUntil)
                return buffer;

            lock (_locker_)
            {
                if (_clock_() < validUntil)
                    return buffer;

                validUntil = NewValidUntil();
                buffer = _factory_.Invoke();
            }
            return buffer;
        }

        private DateTime NewValidUntil() => _clock_().Add(_bufferingPeriod_);
    }
}