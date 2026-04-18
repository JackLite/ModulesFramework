using System;

namespace ModulesFramework.Utils
{
    /// <summary>
    ///     Non-alloc enumerator for arrays that may contain null references
    /// </summary>
    public struct NullableArrayEnumerator<T>
    {
        private int _index;
        private readonly T[] _array;

        public NullableArrayEnumerator(T[] array)
        {
            _index = -1;
            _array = array;
        }

        public T Current
        {
            get
            {
                if (_index < 0 || _array == null)
                    throw new InvalidOperationException();

                return _array[_index];
            }
        }

        public bool MoveNext()
        {
            ++_index;
            while (_index < _array.Length && _array[_index] == null)
                ++_index;

            if (_index >= _array.Length)
                return false;

            return true;
        }

        public void Reset()
        {
            _index = 0;
        }
    }
}