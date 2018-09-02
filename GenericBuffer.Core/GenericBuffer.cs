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
        public GenericBuffer(Func<T> factory, TimeSpan bufferingPeriod, Func<DateTime> clock)
            : this(
                  factory: Convert_FuncTToT_To_FuncT(factory),
                  initialValue: default,
                  bufferingPeriod: bufferingPeriod,
                  clock: clock)
        { }
        public GenericBuffer(Func<T, T> factory, T initialValue, TimeSpan bufferingPeriod) : this(factory, initialValue, bufferingPeriod, () => DateTime.Now) { }

        public GenericBuffer(Func<T, T> factory, T initialValue, TimeSpan bufferingPeriod, Func<DateTime> clock)
        {
            _factory_ = factory ?? throw new ArgumentNullException(nameof(factory));
            _clock_ = clock ?? throw new ArgumentNullException(nameof(clock)); ;
            _bufferingPeriod_ = bufferingPeriod;
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

        public T ForceRefresh() => Refresh(considerBuffer: false);

        public T GetValue()
        {
            if (_clock_() < _validUntil)
                return _buffer;
            return Refresh(considerBuffer: true);
        }

        private T Refresh(bool considerBuffer)
        {
            lock (_locker_)
            {
                if (considerBuffer && _clock_() < _validUntil)
                    return _buffer;

                _buffer = _factory_(_buffer);
                _validUntil = NewValidUntil();

                return _buffer;
            }
        }

        private DateTime NewValidUntil() => _clock_().Add(_bufferingPeriod_);
        private static Func<T, T> Convert_FuncTToT_To_FuncT(Func<T> func)
        {
            if (func == null)
                throw new ArgumentNullException(nameof(func));
            return _ => func();
        }
    }
}