using System;

namespace ModulesFramework.Data.World
{
    public partial class DataWorld
    {
        /// <summary>
        /// Create one data container
        /// </summary>
        /// <typeparam name="T">Type of data in container</typeparam>
        /// <seealso cref="CreateOneData{T}()"/>
        public EcsOneData<T> CreateOneData<T>() where T : struct
        {
            var oneData = new EcsOneData<T>();
            _oneDatas[typeof(T)] = oneData;
            OnOneDataCreated?.Invoke(typeof(T), _oneDatas[typeof(T)]);
            return oneData;
        }

        /// <summary>
        /// Create one data container and set data
        /// </summary>
        /// <param name="data">Data that will be set in container</param>
        /// <typeparam name="T">Type of data in container</typeparam>
        /// <seealso cref="CreateOneData{T}(T)"/>
        public void CreateOneData<T>(T data) where T : struct
        {
            var oneData = new EcsOneData<T>();
            oneData.SetDataIfNotExist(data);
            _oneDatas[typeof(T)] = oneData;
            OnOneDataCreated?.Invoke(typeof(T), oneData);
        }

        internal OneData? GetOneData(Type containerType)
        {
            var dataType = containerType.GetGenericArguments()[0];
            if (_oneDatas.TryGetValue(dataType, out var data))
                return data;

            return null;
        }

        /// <summary>
        /// Allow to get one data container by type
        /// If one data component does not exist it create it
        /// </summary>
        /// <typeparam name="T">Type of one data</typeparam>
        /// <returns>Generic container with one data</returns>
        private EcsOneData<T> GetOneData<T>() where T : struct
        {
            var dataType = typeof(T);
            if (!_oneDatas.TryGetValue(dataType, out var oneData))
                return CreateOneData<T>();

            return (EcsOneData<T>)oneData;
        }

        /// <summary>
        /// Return ref to one data component by T
        /// If one data component does not exist it create it
        /// </summary>
        /// <typeparam name="T">Type of one data</typeparam>
        /// <returns>Ref to one data component</returns>
        public ref T OneData<T>() where T : struct
        {
            var container = GetOneData<T>();
            return ref container.GetData();
        }

        /// <summary>
        /// Fully remove one data.
        /// If you will use <see cref="OneData{T}"/> after removing it returns default value for type
        /// </summary>
        /// <typeparam name="T">Type of one data</typeparam>
        public void RemoveOneData<T>() where T : struct
        {
            if (_oneDatas.ContainsKey(typeof(T)))
            {
                _oneDatas.Remove(typeof(T));
                OnOneDataRemoved?.Invoke(typeof(T));
            }
        }

        /// <summary>
        ///     Check if one data exists. You do not need this check when you get one data
        ///     cause it will be created with default fields. But in some cases you need to know if
        ///     one data was created. For example if it created by some async operations and you can't use await.
        /// </summary>
        /// <typeparam name="T">Type of one data</typeparam>
        /// <returns>True if created</returns>
        public bool IsOneDataExists<T>() where T : struct
        {
            return _oneDatas.ContainsKey(typeof(T));
        }
    }
}