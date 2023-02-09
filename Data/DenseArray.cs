using System;

namespace ModulesFramework.Data
{
    public class DenseArray<T> where T : struct
    {
        private Memory<T> _dataMem;

        public int Length { get; private set; }

        public DenseArray(int capacity = 64)
        {
            Length = 0;
            _dataMem = new Memory<T>(new T[capacity]);
        }

        public int AddData(T data)
        {
            if (Length >= _dataMem.Length)
            {
                var newArr = new T[Length * 2];
                var newMem = new Memory<T>(newArr);
                _dataMem.CopyTo(newMem);
                _dataMem = newMem;
            }

            _dataMem.Span[Length] = data;
            var ret = Length;
            Length++;
            return ret;
        }

        public void RemoveData(int index)
        {
            Length--;
            _dataMem.Span[index] = _dataMem.Span[Length];
        }

        public T this[int i] => _dataMem.Span[i];

        public ref T At(int i)
        {
            return ref _dataMem.Span[i];
        }
        
        public Span<T> GetData()
        {
            return _dataMem.Slice(0, Length).Span;
        }
    }
}