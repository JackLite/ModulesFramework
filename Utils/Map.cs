using ModulesFramework.Utils.Types;
using System;
using System.Collections.Generic;

namespace ModulesFramework.Utils
{
    /// <summary>
    ///     Fast map to get things by type
    /// </summary>
    internal class Map<T> where T : class
    {
        private readonly T[] _existed;

        public IEnumerable<T> Values
        {
            get
            {
                foreach (var value in _existed)
                {
                    if (value != null)
                        yield return value;
                }
            }
        }

        public Map()
        {
            _existed = new T[64];
        }

        public void Add<TType>(T value)
        {
            var index = TypeID<T, TType>.id;
            if (index < 0)
            {
                index = TypeIDCollection<T>.Add<TType>();
            }
            _existed[index] = value;
        }

        public void AddOrReplace<TType>(T value)
        {
            if (TryGet<TType>(out var _))
            {
                var index = TypeID<T, TType>.id;
                _existed[index] = value;
            }
            else
            {
                Add<TType>(value);
            }
        }


        /// <summary>
        ///     Fast get element by TType
        /// </summary>
        public bool TryGet<TType>(out T value)
        {
            value = null;
            var idx = TypeID<T, TType>.id;
            if (idx < 0)
                return false;
            value = _existed[idx];
            return value != null;
        }

        /// <summary>
        ///     Finds element by finder
        ///     Note: O(n)
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        public T Find(Func<T, bool> finder)
        {
            for (var i = 0; i < _existed.Length; i++)
                if (finder(_existed[i]))
                    return _existed[i];
            return null;
        }

        /// <summary>
        ///     Fast remove of element by TType
        /// </summary>
        public void Remove<TType>()
        {
            var idx = TypeID<T, TType>.id;
            _existed[idx] = null;
        }


        /// <summary>
        ///     Remove element found by finder
        ///     Return true if element was found and removed
        ///     Note: O(n)
        /// </summary>
        public bool Remove(Func<T, bool> finder)
        {
            for (var i = 0; i < _existed.Length; i++)
            {
                if (finder(_existed[i]))
                {
                    _existed[i] = null;
                    return true;
                }
            }

            return false;
        }
    }
}