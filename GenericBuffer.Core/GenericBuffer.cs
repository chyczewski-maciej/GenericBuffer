using System;

namespace GenericBuffer.Core
{
    public class GenericBuffer<T> : IGenericBuffer<T>
    {
        private readonly Func<T, T> _factory;
        private readonly TimeSpan _bufferingPeriod;
        private readonly Func<DateTime> _clock;

        private readonly object _locker = new object();
        private DateTime _validUntil = DateTime.MinValue;
        private T _buffer;

        public GenericBuffer(Func<T> factory, TimeSpan bufferingPeriod) : this(factory, bufferingPeriod, () => DateTime.UtcNow) { }
        public GenericBuffer(Func<T> factory, TimeSpan bufferingPeriod, Func<DateTime> clock)
            : this(
                factory: Convert_FuncTToT_To_FuncT(factory),
                initialValue: default,
                bufferingPeriod: bufferingPeriod,
                clock: clock)
        { }

        public GenericBuffer(Func<T, T> factory, T initialValue, TimeSpan bufferingPeriod) : this(factory, initialValue, bufferingPeriod, () => DateTime.UtcNow) { }

        public GenericBuffer(Func<T, T> factory, T initialValue, TimeSpan bufferingPeriod, Func<DateTime> clock)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _bufferingPeriod = bufferingPeriod;
            _buffer = initialValue;
        }

        public void Reset()
        {
            lock (_locker)
            {
                _buffer = default;
                _validUntil = DateTime.MinValue;
            }
        }

        public T ForceRefresh() => Refresh(considerBuffer: false);

        public T GetValue()
        {
            if (_clock() < _validUntil)
                return _buffer;
            return Refresh(considerBuffer: true);
        }

        private T Refresh(bool considerBuffer)
        {
            lock (_locker)
            {
                if (considerBuffer && _clock() < _validUntil)
                    return _buffer;

                _buffer = _factory(_buffer);
                _validUntil = NewValidUntil();

                return _buffer;
            }
        }

        private DateTime NewValidUntil() => _clock().Add(_bufferingPeriod);
        private static Func<T, T> Convert_FuncTToT_To_FuncT(Func<T> func)
        {
            if (func == null)
                throw new ArgumentNullException(nameof(func));
            return _ => func();
        }
    }
}