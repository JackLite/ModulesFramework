﻿#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ModulesFramework.Data.Enumerators;
using ModulesFramework.Modules;
#if MODULES_DEBUG
using ModulesFramework.Exceptions;
#endif

namespace ModulesFramework.Data
{
    public partial class DataWorld
    {
        private int _entityCount;
        private readonly Dictionary<Type, EcsTable> _data = new Dictionary<Type, EcsTable>();
        private readonly EcsTable<Entity> _entitiesTable = new EcsTable<Entity>();
        private readonly EcsTable<EntityGeneration> _generationsTable = new EcsTable<EntityGeneration>();
        private readonly Queue<int> _freeEid = new Queue<int>(64);
        private readonly Dictionary<Type, OneData> _oneDatas = new Dictionary<Type, OneData>();

        private readonly Stack<Query> _queriesPool;

        public event Action<int>? OnEntityCreated;
        public event Action<int>? OnEntityChanged;
        public event Action<int>? OnEntityDestroyed;

        internal event Action<Type, OneData>? OnOneDataCreated;
        internal event Action<Type>? OnOneDataRemoved;

        public DataWorld(int worldIndex)
        {
            _modules = new Dictionary<Type, EcsModule>();
            _submodules = new Dictionary<Type, List<EcsModule>>();
            CtorModules(worldIndex);
            _queriesPool = new Stack<Query>(128);
        }

        public Entity NewEntity()
        {
            var entity = new Entity
            {
                World = this
            };
            if (_freeEid.Count == 0)
            {
                entity.Id = _entityCount;
                _generationsTable.AddData(entity.Id, new EntityGeneration
                {
                    generation = 0
                });
                entity.generation = 0;
                ++_entityCount;
            }
            else
            {
                entity.Id = _freeEid.Dequeue();
                ref var generation = ref _generationsTable.GetData(entity.Id);
                generation.generation++;
                entity.generation = generation.generation;
            }

            _entitiesTable.AddData(entity.Id, entity);

            #if MODULES_DEBUG
            Logger.LogDebug($"Entity {entity.Id.ToString()} created", LogFilter.EntityLife);
            #endif

            OnEntityCreated?.Invoke(entity.Id);
            return entity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddComponent<T>(int eid, T component) where T : struct
        {
            var table = GetEcsTable<T>();

            #if !MODULES_OPT
            if (table.Contains(eid))
                Logger.LogWarning($"Component {typeof(T).Name} exists in {eid.ToString()} entity and will be replaced");
            #endif

            table.Remove(eid);
            table.AddData(eid, component);

            #if MODULES_DEBUG
            Logger.LogDebug($"Add to {eid.ToString()} {typeof(T).Name} component", LogFilter.EntityModifications);
            #endif

            OnEntityChanged?.Invoke(eid);
        }

        public void AddNewComponent<T>(int eid, T component) where T : struct
        {
            var table = GetEcsTable<T>();

            table.AddNewData(eid, component);

            #if MODULES_DEBUG
            Logger.LogDebug($"Add to {eid.ToString()} new {typeof(T).Name} component", LogFilter.EntityModifications);
            #endif

            OnEntityChanged?.Invoke(eid);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveComponent<T>(int eid) where T : struct
        {
            GetEcsTable<T>().Remove(eid);
            #if MODULES_DEBUG
            Logger.LogDebug($"Remove from {eid.ToString()} {typeof(T).Name} component", LogFilter.EntityModifications);
            #endif
            OnEntityChanged?.Invoke(eid);
        }

        public void RemoveFirstComponent<T>(int eid) where T : struct
        {
            GetEcsTable<T>().RemoveFirst(eid);
            #if MODULES_DEBUG
            Logger.LogDebug($"Remove from {eid.ToString()} first {typeof(T).Name} component",
                LogFilter.EntityModifications);
            #endif
            OnEntityChanged?.Invoke(eid);
        }

        internal void RemoveAt<T>(int eid, int mtmIndex) where T : struct
        {
            GetEcsTable<T>().RemoveAt(eid, mtmIndex);
            #if MODULES_DEBUG
            Logger.LogDebug(
                $"Remove from {eid.ToString()} {typeof(T).Name} at multiple index {mtmIndex}",
                LogFilter.EntityModifications
            );
            #endif
            OnEntityChanged?.Invoke(eid);
        }

        public void RemoveAll<T>(int eid) where T : struct
        {
            GetEcsTable<T>().RemoveAll(eid);
            #if MODULES_DEBUG
            Logger.LogDebug($"Remove all {typeof(T).Name} components from {eid.ToString()}",
                LogFilter.EntityModifications);
            #endif
            OnEntityChanged?.Invoke(eid);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetComponent<T>(int id) where T : struct
        {
            return ref GetEcsTable<T>().GetData(id);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MultipleComponentsIndicesEnumerable<T> GetIndices<T>(int eid) where T : struct
        {
            return GetEcsTable<T>().GetMultipleIndices(eid);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetComponentAt<T>(int eid, int index) where T : struct
        {
            return ref GetEcsTable<T>().MultipleAt(eid, index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MultipleComponentsEnumerable<T> GetAllComponents<T>(int eid) where T : struct
        {
            return GetEcsTable<T>().GetMultipleForEntity(eid);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsEntityExists(int eid)
        {
            return _entitiesTable.Contains(eid);
        }

        public bool IsEntityAlive(Entity entity)
        {
            if (!IsEntityExists(entity.Id))
                return false;
            var generation = _generationsTable.GetData(entity.Id);
            return generation.generation == entity.generation;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsTable<T> GetEcsTable<T>() where T : struct
        {
            CreateTableIfNeed<T>();
            return (EcsTable<T>)_data[typeof(T)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsTable GetEcsTable(Type type)
        {
            return _data[type];
        }

        public Span<T> GetRawData<T>() where T : struct
        {
            return GetEcsTable<T>().GetRawData();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Query Select<T>() where T : struct
        {
            var table = GetEcsTable<T>();
            return GetQuery(table);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Exist<T>() where T : struct
        {
            var table = GetEcsTable<T>();
            var query = GetQuery(table);
            return query.Any();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySelectFirst<T>(out T c) where T : struct
        {
            var table = GetEcsTable<T>();
            var query = GetQuery(table);
            return query.TrySelectFirst(out c);
        }

        private void ReturnQuery(Query query)
        {
            _queriesPool.Push(query);
        }

        private Query GetQuery<T>(EcsTable<T> table) where T : struct
        {
            if (_queriesPool.Count == 0)
                FillQueriesPool();
            var query = _queriesPool.Pop();
            query.Init(table);
            return query;
        }

        private void FillQueriesPool()
        {
            while (_queriesPool.Count < 128)
                _queriesPool.Push(new Query(this));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsTable<T> CreateTableIfNeed<T>() where T : struct
        {
            var type = typeof(T);
            if (!_data.ContainsKey(type))
                _data[type] = new EcsTable<T>();
            return (EcsTable<T>)_data[type];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Entity GetEntity(int id)
        {
            return _entitiesTable.GetData(id);
        }

        internal Entity GetEntityByDenseIndex<T>(int denseIndex) where T : struct
        {
            return GetEntity(GetEcsTable<T>().GetEidByIndex(denseIndex));
        }

        public bool HasComponent<T>(int id) where T : struct
        {
            return GetEcsTable<T>().Contains(id);
        }

        public bool HasComponent(int eid, Type componentType)
        {
            if (!_data.ContainsKey(componentType))
                return false;
            return _data[componentType].Contains(eid);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DestroyEntity(int id)
        {
            foreach (var table in _data.Values)
            {
                table.RemoveInternal(id);
            }

            _entitiesTable.Remove(id);
            _freeEid.Enqueue(id);
            #if MODULES_DEBUG
            Logger.LogDebug($"Entity {id.ToString()} destroyed", LogFilter.EntityLife);
            #endif
            OnEntityDestroyed?.Invoke(id);
        }

        internal void MapTables(Action<Type, EcsTable> handler)
        {
            foreach (var kvp in _data)
            {
                handler.Invoke(kvp.Key, kvp.Value);
            }
        }


        /// <summary>
        /// Create one data container
        /// </summary>
        /// <typeparam name="T">Type of data in container</typeparam>
        /// <seealso cref="CreateOneData{T}()"/>
        public void CreateOneData<T>() where T : struct
        {
            _oneDatas[typeof(T)] = new EcsOneData<T>();
            OnOneDataCreated?.Invoke(typeof(T), _oneDatas[typeof(T)]);
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
            if (_oneDatas.ContainsKey(dataType))
                return _oneDatas[dataType];

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
            if (!_oneDatas.ContainsKey(dataType))
            {
                CreateOneData<T>();
            }

            return (EcsOneData<T>)_oneDatas[dataType];
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

        /// <summary>
        ///     Return count of multiple components at entity
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CountComponentsAt<T>(int eid) where T : struct
        {
            return GetEcsTable<T>().GetMultipleDataLength(eid);
        }

        /// <summary>
        ///     Create custom index for component, so you can get component or entity by component's index
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CreateIndex<T, TIndex>(Func<T, TIndex> getIndex) where T : struct where TIndex : notnull
        {
            GetEcsTable<T>().CreateIndex(getIndex);
        }

        /// <summary>
        ///     Return component by custom index. Throws exception if there is no index or component
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T ComponentByCustomIndex<T, TIndex>(TIndex index) where T : struct where TIndex : notnull
        {
            return ref GetEcsTable<T>().ComponentByCustomIndex(index);
        }
        
        /// <summary>
        ///     Check if there is component with given index
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsCustomIndexExists<T, TIndex>(TIndex index) where T : struct where TIndex : notnull
        {
            return GetEcsTable<T>().HasCustomIndex(index);
        }

        /// <summary>
        ///     Return entity by custom index on some component on entity.
        ///     Returns nul if there is no entity found
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Entity? FindEntityByCustomIndex<T, TIndex>(TIndex index) where T : struct where TIndex : notnull
        {
            var eid = GetEcsTable<T>().FindEidByCustomIndex(index);
            if (eid == null)
                return null;
            return GetEntity(eid.Value);
        }

        /// <summary>
        ///     Updates custom index for component. It's remove old index first and then create new one
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateCustomIndex<T, TIndex>(TIndex oldIndex, T testComponent, int eid)
            where T : struct
            where TIndex : notnull
        {
            GetEcsTable<T>().UpdateCustomIndex(oldIndex, testComponent, eid);
        }
    }
}