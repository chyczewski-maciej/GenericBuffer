﻿using System;

namespace GenericBuffer.Core
{
    public class GenericBuffer<T>
    {
        private readonly Func<T> _factory_;
        private readonly TimeSpan _bufferingPeriod_;

        private readonly object _locker_ = new object();

        private DateTime validUntil = DateTime.MinValue;
        private T buffer;

        public GenericBuffer(Func<T> factory, TimeSpan bufferingPeriod)
        {
            _factory_ = factory ?? throw new ArgumentNullException(nameof(factory));
            _bufferingPeriod_ = bufferingPeriod;
        }

        public void Reset()
        {
            lock (_locker_)
            {
                buffer = default;
                validUntil = DateTime.MinValue;
            }
        }

        public void ForceRefresh()
        {
            lock (_locker_)
            {
                buffer = _factory_();
                validUntil = NewValidUntil();
            }
        }

        public T GetValue()
        {
            if (DateTime.Now < validUntil)
                return buffer;

            lock (_locker_)
            {
                if (DateTime.Now < validUntil)
                    return buffer;

                validUntil = NewValidUntil();
                buffer = _factory_.Invoke();
            }
            return buffer;
        }

        private DateTime NewValidUntil() => DateTime.Now.Add(_bufferingPeriod_);
    }
}