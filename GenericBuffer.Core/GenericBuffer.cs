using System;

namespace GenericBuffer.Core
{
    public class GenericBuffer<T> : IGenericBuffer<T>
    {
        private readonly Func<T, T> _factory_;
        private readonly TimeSpan _bufferingPeriod_;
        private readonly Func<DateTime> _clock_;

        private readonly object _locker_ = new object();
        private DateTime _validUntil = DateTime.MinValue;
        private T _buffer;

        public GenericBuffer(Func<T> factory, TimeSpan bufferingPeriod) : this(factory, bufferingPeriod, () => DateTime.Now) { }
        public GenericBuffer(Func<T> factory, TimeSpan bufferingPeriod, Func<DateTime> clock) : this(_ => factory(), bufferingPeriod, default, clock) { }
        public GenericBuffer(Func<T, T> factory, TimeSpan bufferingPeriod, T initialValue) : this(factory, bufferingPeriod, initialValue, () => DateTime.Now) { }

        public GenericBuffer(Func<T, T> factory, TimeSpan bufferingPeriod, T initialValue, Func<DateTime> clock)
        {
            _factory_ = factory ?? throw new ArgumentNullException(nameof(factory));
            _bufferingPeriod_ = bufferingPeriod;
            _clock_ = clock;
            _buffer = initialValue;
        }

        public void Reset()
        {
            lock (_locker_)
            {
                _buffer = default;
                _validUntil = DateTime.MinValue;
            }
        }

        public T ForceRefresh()
        {
            lock (_locker_)
            {
                _buffer = _factory_(_buffer);
                _validUntil = NewValidUntil();
            }

            return _buffer;
        }

        public T GetValue()
        {
            if (_clock_() < _validUntil)
                return _buffer;

            lock (_locker_)
            {
                if (_clock_() < _validUntil)
                    return _buffer;

                return ForceRefresh();
            }
        }

        private DateTime NewValidUntil() => _clock_().Add(_bufferingPeriod_);
    }
}