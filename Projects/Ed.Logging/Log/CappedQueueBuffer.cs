using System;
using System.Collections;
using System.Collections.Generic;

namespace Ed.Logging.Log
{
    /// <inheritdoc />
    /// <summary>  Just a once more capped queue buffer generic </summary>
    internal class CappedQueueBuffer<T> : IEnumerable<T>
    {
        private readonly T[] _array;
        private int _startIndex;

        /// <summary> Ctor with buffer (.Net array) capacity, immediately allocated</summary>
        public CappedQueueBuffer(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            _array = new T[capacity];
            _startIndex = 0;
            Length = 0;
        }

        /// <summary>  Capacity is just the array length</summary>
        public int Capacity => _array.Length;

        /// <summary>  Length could be only less then capacity (that's characteristic for such buffer) </summary>
        public int Length { get; private set; }

        public IEnumerator<T> GetEnumerator()
        {
            for (var i = 0; i < Length; ++i)
                yield return GetElementUnchecked(i);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>   Pushes element back</summary>
        public void PushBack(T newItem)
        {
            _array[_startIndex] = newItem;

            if (_startIndex == Capacity - 1)
                _startIndex = 0;
            else
                _startIndex++;

            if (Length < Capacity)
                ++Length;
        }

        /// <summary>   Gets an element without boundary check </summary>
        private T GetElementUnchecked(int index)
        {
            var index2 = Length < Capacity ? index : (_startIndex + index) % Capacity;

            return _array[index2];
        }
    }
}