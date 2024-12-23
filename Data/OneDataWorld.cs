﻿using System;
using System.Collections.Generic;

namespace ModulesFramework.Data
{
    public partial class DataWorld
    {
        private readonly Dictionary<Type, OneData> _oneDatas = new Dictionary<Type, OneData>();

        internal IEnumerable<OneData> OneDataCollection => _oneDatas.Values;
        /// <summary>
        /// Create one data container
        /// </summary>
        /// <typeparam name="T">Type of data in container</typeparam>
        /// <seealso cref="CreateOneData{T}()"/>
        public ref T CreateOneData<T>() where T : struct
        {
            return ref CreateOneData<T>(default);
        }

        /// <summary>
        /// Create one data container and set data
        /// </summary>
        /// <param name="data">Data that will be set in container</param>
        /// <typeparam name="T">Type of data in container</typeparam>
        /// <seealso cref="CreateOneData{T}(T)"/>
        public ref T CreateOneData<T>(T data) where T : struct
        {
            return ref CreateOneData(data, true);
        }

        private ref T CreateOneData<T>(T data, bool updateGeneration) where T : struct
        {
            var oneData = new EcsOneData<T>();
            oneData.SetDataIfNotExist(data);

            if (_oneDatas.TryGetValue(typeof(T), out var oldData))
                oneData.generation = oldData.generation;

            if (updateGeneration)
                oneData.generation++;

            _oneDatas[typeof(T)] = oneData;
            OnOneDataCreated?.Invoke(typeof(T), oneData);
            return ref oneData.GetData();
        }

        internal OneData? GetOneData(Type containerType)
        {
            var dataType = containerType.GetGenericArguments()[0];
            if (_oneDatas.TryGetValue(dataType, out var data))
                return data;

            return null;
        }

        public OneData? GetOneDataWrapper(Type dataType)
        {
            if (_oneDatas.TryGetValue(dataType, out var data))
                return data;

            return null;
        }

        /// <summary>
        /// Return ref to one data component by T
        /// If one data component does not exist it create it
        /// </summary>
        /// <typeparam name="T">Type of one data</typeparam>
        /// <returns>Ref to one data component</returns>
        public ref T OneData<T>() where T : struct
        {
            var dataType = typeof(T);
            if (!_oneDatas.TryGetValue(dataType, out var oneData))
                return ref CreateOneData<T>();

            return ref ((EcsOneData<T>)oneData).GetData();
        }

        /// <summary>
        /// Return number of one data generation. It starts from 0 like in <see cref="Entity"/>
        /// </summary>
        /// <typeparam name="T">Type of one data</typeparam>
        /// <returns>Number of generation or -1 if one data wasn't created yet</returns>
        public int OneDataGeneration<T>() where T : struct
        {
            if (_oneDatas.TryGetValue(typeof(T), out var data))
                return data.generation;
            return -1;
        }

        public ref T ReplaceOneData<T>(T data) where T : struct
        {
            return ref CreateOneData(data, false);
        }

        public ref T ReplaceOneData<T>() where T : struct
        {
            return ref CreateOneData<T>(default, false);
        }

        /// <summary>
        /// Fully remove one data.
        /// If you will use <see cref="OneData{T}"/> after removing it returns default value for type
        /// </summary>
        /// <typeparam name="T">Type of one data</typeparam>
        public void RemoveOneData<T>() where T : struct
        {
            RemoveOneData(typeof(T));
        }

        /// <summary>
        /// Fully remove one data.
        /// If you will use <see cref="OneData{T}"/> after removing it returns default value for type
        /// </summary>
        public void RemoveOneData(Type type)
        {
            if (_oneDatas.ContainsKey(type))
            {
                _oneDatas.Remove(type);
                OnOneDataRemoved?.Invoke(type);
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