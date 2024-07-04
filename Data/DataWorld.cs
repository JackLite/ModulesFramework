#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ModulesFramework.Data.Enumerators;
using ModulesFramework.Exceptions;
using ModulesFramework.Modules;
using ModulesFramework.Utils;
#if MODULES_DEBUG
using ModulesFramework.Exceptions;
#endif

namespace ModulesFramework.Data
{
    public partial class DataWorld
    {
        private readonly AssemblyFilter _assemblyFilter;
        private int _entityCount;
        private readonly Dictionary<Type, EcsTable> _data = new Dictionary<Type, EcsTable>();
        private readonly EcsTable<Entity> _entitiesTable = new EcsTable<Entity>();
        private readonly EcsTable<EntityGeneration> _generationsTable = new EcsTable<EntityGeneration>();
        private readonly Queue<int> _freeEid = new Queue<int>(64);

        private readonly Stack<DataQuery> _queriesPool;

        public event Action<int>? OnEntityCreated;
        public event Action<int>? OnEntityChanged;
        public event Action<int>? OnEntityDestroyed;

        internal event Action<Type, OneData>? OnOneDataCreated;
        internal event Action<Type>? OnOneDataRemoved;

        public DataWorld(int worldIndex, AssemblyFilter assemblyFilter)
        {
            _assemblyFilter = assemblyFilter;
            _modules = new Dictionary<Type, EcsModule>();
            _submodules = new Dictionary<Type, List<EcsModule>>();
            CtorModules(worldIndex);
            _queriesPool = new Stack<DataQuery>(128);
        }

        /// <summary>
        ///     Creates new Entity and returns it
        ///     Rises <see cref="OnEntityCreated"/> after creation
        ///     <seealso cref="Entity"/>
        /// </summary>
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

        /// <summary>
        ///     Adds component to entity. If component exists it will be replaced
        /// </summary>
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

        /// <summary>
        ///     Adds new component to entity.
        ///     This is MultipleComponents API and allowed to add multiple components to a single entity
        /// </summary>
        public void AddNewComponent<T>(int eid, T component) where T : struct
        {
            var table = GetEcsTable<T>();

            table.AddNewData(eid, component);

            #if MODULES_DEBUG
            Logger.LogDebug($"Add to {eid.ToString()} new {typeof(T).Name} component", LogFilter.EntityModifications);
            #endif

            OnEntityChanged?.Invoke(eid);
        }

        /// <summary>
        ///     Remove component from entity. If there is no component it will do nothing
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveComponent<T>(int eid) where T : struct
        {
            GetEcsTable<T>().Remove(eid);
            #if MODULES_DEBUG
            Logger.LogDebug($"Remove from {eid.ToString()} {typeof(T).Name} component", LogFilter.EntityModifications);
            #endif
            OnEntityChanged?.Invoke(eid);
        }

        /// <summary>
        ///     Remove first component from entity.
        ///     This is MultipleComponents API and allowed to remove one component from an entity
        /// </summary>
        public void RemoveFirstComponent<T>(int eid) where T : struct
        {
            GetEcsTable<T>().RemoveFirst(eid);
            #if MODULES_DEBUG
            Logger.LogDebug($"Remove from {eid.ToString()} first {typeof(T).Name} component",
                LogFilter.EntityModifications);
            #endif
            OnEntityChanged?.Invoke(eid);
        }

        /// <summary>
        ///     Remove all components of type T from entity
        ///     This is MultipleComponents API and allowed to remove all components from an entity
        /// </summary>
        public void RemoveAll<T>(int eid) where T : struct
        {
            GetEcsTable<T>().RemoveAll(eid);
            #if MODULES_DEBUG
            Logger.LogDebug($"Remove all {typeof(T).Name} components from {eid.ToString()}",
                LogFilter.EntityModifications);
            #endif
            OnEntityChanged?.Invoke(eid);
        }

        /// <summary>
        ///     Get component from entity by type.
        ///     If there is no component it will throw <see cref="DataNotExistsInTableException{T}"/>
        /// </summary>
        /// <param name="id"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetComponent<T>(int id) where T : struct
        {
            return ref GetEcsTable<T>().GetData(id);
        }

        /// <summary>
        ///     Return indices enumerable to iterate through multiple components.
        ///     This is MultipleComponents API
        /// </summary>
        /// <param name="eid"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MultipleComponentsIndicesEnumerable<T> GetIndices<T>(int eid) where T : struct
        {
            return GetEcsTable<T>().GetMultipleIndices(eid);
        }

        /// <summary>
        ///     Return component by inner index.
        ///     This is MultipleComponents API
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetComponentAt<T>(int eid, int index) where T : struct
        {
            return ref GetEcsTable<T>().MultipleAt(eid, index);
        }

        /// <summary>
        ///     Return enumerable of all components of type T from entity.
        ///     This is MultipleComponents API
        /// </summary>
        /// <param name="eid"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MultipleComponentsEnumerable<T> GetAllComponents<T>(int eid) where T : struct
        {
            return GetEcsTable<T>().GetMultipleForEntity(eid);
        }

        /// <summary>
        ///     Return true if entity exists. Entity may exists but has another generation
        ///     <seealso cref="IsEntityAlive"/>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsEntityExists(int eid)
        {
            return _entitiesTable.Contains(eid);
        }

        /// <summary>
        ///     Return true if entity exists. Entity may exists but has another generation
        ///     <seealso cref="IsEntityAlive"/>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsEntityExists(Entity entity)
        {
            return _entitiesTable.Contains(entity.Id);
        }

        /// <summary>
        ///     Return true if entity wasn't deleted
        /// </summary>
        public bool IsEntityAlive(Entity entity)
        {
            if (!IsEntityExists(entity.Id))
                return false;
            var generation = _generationsTable.GetData(entity.Id);
            return generation.generation == entity.generation;
        }

        /// <summary>
        ///     Return true if entity wasn't deleted
        /// </summary>
        public bool IsEntityAlive(int eid)
        {
            if (!IsEntityExists(eid))
                return false;
            var generation = _generationsTable.GetData(eid);
            var entity = _entitiesTable.GetData(eid);
            return generation.generation == entity.generation;
        }

        /// <summary>
        ///     Return EcsTable by type. If table doesn't exist it will be created
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsTable<T> GetEcsTable<T>() where T : struct
        {
            return CreateTableIfNeed<T>();
        }

        /// <summary>
        ///     Return EcsTable by type. If table doesn't exist - throw exception
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsTable GetEcsTable(Type type)
        {
            return _data[type];
        }

        /// <summary>
        ///     Return dense array of components by type.
        ///     This is the fastest way to iterate through components
        /// </summary>
        public Span<T> GetRawData<T>() where T : struct
        {
            return GetEcsTable<T>().GetRawData();
        }

        /// <summary>
        ///     Return query to select entities or components.
        ///     It will take query from pool, so it's a good practice to use 'using' keyword
        ///     for the sake of decreasing memory allocations
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DataQuery Select<T>() where T : struct
        {
            var table = GetEcsTable<T>();
            return GetQuery(table);
        }

        /// <summary>
        ///     Return true if there is at least one component of type T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Exist<T>() where T : struct
        {
            var table = GetEcsTable<T>();
            var query = GetQuery(table);
            return query.Any();
        }

        /// <summary>
        ///     Return Query to pool. You may need it if you don't use 'using' with <see cref="Select{T}"/>
        /// </summary>
        internal void ReturnQuery(DataQuery query)
        {
            _queriesPool.Push(query);
        }

        private DataQuery GetQuery<T>(EcsTable<T> table) where T : struct
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
                _queriesPool.Push(new DataQuery(this));
        }

        /// <summary>
        ///     Create EcsTable if there is no one
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsTable<T> CreateTableIfNeed<T>() where T : struct
        {
            var type = typeof(T);
            if (_data.TryGetValue(type, out var table))
            {
                return (EcsTable<T>)table;
            }

            var newTable = new EcsTable<T>();
            _data[type] = newTable;
            return newTable;
        }

        /// <summary>
        ///     Return Entity by id
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Entity GetEntity(int id)
        {
            return _entitiesTable.GetData(id);
        }

        /// <summary>
        ///     Return Entity by internal index. It isn't safe cause if entity will be destroyed the dense array
        ///     will change and it may caused unexpected behaviour and bugs that will be very hard to find.
        /// </summary>
        internal Entity GetEntityByDenseIndex<T>(int denseIndex) where T : struct
        {
            return GetEntity(GetEcsTable<T>().GetEidByIndex(denseIndex));
        }

        /// <summary>
        ///     Return true if entity with specified id has component of type T
        /// </summary>
        public bool HasComponent<T>(int id) where T : struct
        {
            return GetEcsTable<T>().Contains(id);
        }

        /// <summary>
        ///     Return true if entity with specified id has component of specified Type
        /// </summary>
        public bool HasComponent(int eid, Type componentType)
        {
            if (!_data.ContainsKey(componentType))
                return false;
            return _data[componentType].Contains(eid);
        }

        /// <summary>
        ///     Destroy entity and remove all of its components
        /// </summary>
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
        ///     Return count of multiple components at entity. This is MultipleComponents API
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CountComponentsAt<T>(int eid) where T : struct
        {
            return GetEcsTable<T>().GetMultipleDataLength(eid);
        }

        /// <summary>
        ///     Return true if there is no components bound to entity with specified id
        /// </summary>
        public bool IsEmptyEntity(int id)
        {
            foreach (var value in _data.Values)
            {
                if (value.Contains(id))
                    return false;
            }

            return true;
        }

        internal IEnumerable<Entity> GetAliveEntities()
        {
            foreach (var entity in _entitiesTable.GetInternalData())
            {
                if(!IsEntityAlive(entity))
                    continue;
                yield return entity;
            }
        }
    }
}