using System.Collections;
using System.Diagnostics.Contracts;

namespace SnakeAlgorithm
{
    public class CyclicArray<T>
    {
        private T[] _array;
        private int _start;

        public int Length => _array.Length;

        public void ShiftLeft()
        {
            _start++;
            if (_start == Length) _start = 0;
        }
        public void ShiftRight()
        {
            _start--;
            if (_start < 0) _start = Length - 1;
        }
        public void ShiftLeft(int count)
        {
            if (count < 0)
                count = count % Length + Length;
            _start += count;
            _start %= Length;
        }
        public void ShiftRight(int count)
        {
            if (count < 0)
                count = count % Length + Length;
            _start -= count;
            if (_start < 0) _start = (_start % Length + Length) % Length;
        }

        public CyclicArray(int capacity)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than zero.");
            _array = new T[capacity];
            _start = 0;
        }

        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= Length) throw new IndexOutOfRangeException();
                index += _start;
                if (index >= Length) return _array[index - Length];
                return _array[index];
            }
            set
            {
                if (index < 0 || index >= Length) throw new IndexOutOfRangeException();
                index += _start;
                if (index >= Length) _array[index - Length] = value;
                else _array[index] = value;
            }
        }
    }
}