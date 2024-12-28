#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ModulesFramework.Data.Enumerators;
using ModulesFramework.Exceptions;
using ModulesFramework.Modules;
using ModulesFramework.Utils;

namespace ModulesFramework.Data
{
    public partial class DataWorld
    {
        private readonly AssemblyFilter _assemblyFilter;
        private int _entityCount;
        private readonly Map<EcsTable> _data = new Map<EcsTable>();
        private readonly EntityTable _entitiesTable = new EntityTable();
        private readonly EntityGenerationTable _generationsTable = new EntityGenerationTable();
        private readonly Queue<int> _freeEid = new Queue<int>(64);

        private readonly Stack<DataQuery> _queriesPool;

        public event Action<int>? OnEntityCreated;
        public event Action<int>? OnEntityChanged;
        public event Action<int>? OnEntityDestroyed;
        public event Action<int>? OnCustomIdChanged;

        internal event Action<Type, OneData>? OnOneDataCreated;
        internal event Action<Type>? OnOneDataRemoved;

        public DataWorld(int worldIndex, AssemblyFilter assemblyFilter)
        {
            _assemblyFilter = assemblyFilter;
            _modules = new Map<EcsModule>();
            CtorModules(worldIndex);
            _queriesPool = new Stack<DataQuery>(128);
            _entitiesTable.CreateKey(e => e.GetCustomId());
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
            #if MODULES_DEBUG
            if (!IsEntityAlive(eid))
                throw new EntityDestroyedException(eid);
            #endif

            var table = GetEcsTable<T>();

            #if MODULES_DEBUG
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
        ///     Add component by type.
        ///     Use this method only for debugging cause it use reflection in some cases
        /// </summary>
        public void AddComponent(int eid, Type type, object component)
        {
#if MODULES_DEBUG
            if (!IsEntityAlive(eid))
                throw new EntityDestroyedException(eid);
#endif

            var table = GetEcsTable(type);
            if (table == null)
            {
                var tableType = typeof(EcsTable<>).MakeGenericType(type);
                table = (EcsTable)Activator.CreateInstance(tableType, this);
                var meth = _data.GetType().GetMethod(nameof(Map<object>.Add))!.MakeGenericMethod(type);
                meth.Invoke(_data, new[] { table });
            }

#if MODULES_DEBUG
            if (table.Contains(eid))
                Logger.LogWarning($"Component {type.Name} exists in {eid.ToString()} entity and will be replaced");
#endif

            table.Remove(eid);
            table.AddData(eid, component);

#if MODULES_DEBUG
            Logger.LogDebug($"Add to {eid.ToString()} {type.Name} component", LogFilter.EntityModifications);
#endif

            OnEntityChanged?.Invoke(eid);
        }

        /// <summary>
        ///     Adds new component to entity.
        ///     This is MultipleComponents API and allowed to add multiple components to a single entity
        /// </summary>
        public void AddNewComponent<T>(int eid, T component) where T : struct
        {
            #if MODULES_DEBUG
            if (!IsEntityAlive(eid))
                throw new EntityDestroyedException(eid);
            #endif

            var table = GetEcsTable<T>();
            table.AddNewData(eid, component);

            #if MODULES_DEBUG
            Logger.LogDebug($"Add to {eid.ToString()} new {typeof(T).Name} component", LogFilter.EntityModifications);
            #endif

            OnEntityChanged?.Invoke(eid);
        }

        /// <summary>
        ///     Add component by type.
        ///     Use this method only for debugging cause it's slower then <see cref="AddComponent<T>"/>
        /// </summary>
        public void AddNewComponent(int eid, Type type, object component)
        {
#if MODULES_DEBUG
            if (!IsEntityAlive(eid))
                throw new EntityDestroyedException(eid);
#endif

            var table = GetEcsTable(type);
            table.AddNewData(eid, component);

#if MODULES_DEBUG
            Logger.LogDebug($"Add to {eid.ToString()} {type.Name} component", LogFilter.EntityModifications);
#endif

            OnEntityChanged?.Invoke(eid);
        }

        /// <summary>
        ///     Remove component from entity. If there is no component it will do nothing
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveComponent<T>(int eid) where T : struct
        {
            #if MODULES_DEBUG
            if (!IsEntityAlive(eid))
                throw new EntityDestroyedException(eid);
            #endif

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
            #if MODULES_DEBUG
            if (!IsEntityAlive(eid))
                throw new EntityDestroyedException(eid);
            #endif

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
            #if MODULES_DEBUG
            if (!IsEntityAlive(eid))
                throw new EntityDestroyedException(eid);
            #endif

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
            return _data.Find(table => table != null && table.Type == type);
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
            if (_data.TryGet<T>(out var table))
                return (EcsTable<T>)table;

            var newTable = new EcsTable<T>(this);
            _data.Add<T>(newTable);
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
            var table = _data.Find(table => table != null && table.Type == componentType);
            if (table == null)
                return false;
            
            return table.Contains(eid);
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
            foreach (var table in _data.Values)
            {
                handler.Invoke(table.Type, table);
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

        /// <summary>
        ///     Return entity with custom id. If there is no such entity - throw exception
        ///     <seealso cref="IsEntityExists(string)"/>
        /// </summary>
        public Entity EntityByCustomId(string customId)
        {
            return _entitiesTable.ByKey(customId);
        }

        /// <summary>
        ///     Set entity custom id - the string unique key that can be used to find this entity
        /// </summary>
        /// <param name="id">Entity id</param>
        public void SetEntityCustomId(int id, string customId)
        {
            ref var entity = ref _entitiesTable.GetData(id);
            var oldId = entity.GetCustomIdInternal();
            entity.SetCustomIdInternal(customId);
            _entitiesTable.UpdateKey(oldId, entity, entity.Id);
            OnCustomIdChanged?.Invoke(id);
        }

        /// <summary>
        ///     Return true if entity with specified customId exists
        /// </summary>
        public bool IsEntityExists(string customId)
        {
            return _entitiesTable.HasKey(customId);
        }

        /// <summary>
        ///     Return types of single components that entity contains
        /// </summary>
        public IEnumerable<Type> GetEntitySingleComponentsType(int eid)
        {
            foreach (var table in _data.Values)
            {
                if (table.Contains(eid) && !table.IsMultiple)
                    yield return table.Type;
            }
        }

        /// <summary>
        ///     Return types of multiple components that entity contains
        /// </summary>
        public IEnumerable<Type> GetEntityMultipleComponentsType(int eid)
        {
            foreach (var table in _data.Values)
            {
                if (table.Contains(eid) && table.IsMultiple)
                    yield return table.Type;
            }
        }

        public IEnumerable<Entity> GetAliveEntities()
        {
            return _entitiesTable.GetInternalData();
        }

        internal void RiseEntityChanged(int eid)
        {
            OnEntityChanged?.Invoke(eid);
        }
    }
}