#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
#if MODULES_DEBUG
using ModulesFramework.Exceptions;
#endif
using ModulesFramework.Modules;

namespace ModulesFramework.Data
{
    public partial class DataWorld
    {
        private int _entityCount;
        private readonly Dictionary<Type, EcsTable> _data = new Dictionary<Type, EcsTable>();
        private readonly EcsTable<Entity> _entitiesTable = new EcsTable<Entity>();
        private readonly EcsTable<EntityGeneration> _generationsTable = new EcsTable<EntityGeneration>();
        private readonly Stack<int> _freeEid = new Stack<int>(64);
        private readonly Dictionary<Type, EcsModule> _modules;
        private readonly Dictionary<Type, OneData> _oneDatas = new Dictionary<Type, OneData>();

        private readonly Stack<Query> _queriesPool;

        public event Action<int>? OnEntityCreated;
        public event Action<int>? OnEntityChanged;
        public event Action<int>? OnEntityDestroyed;

        internal event Action<Type, OneData>? OnOneDataCreated;

        public DataWorld()
        {
            _modules = CreateAllEcsModules().ToDictionary(m => m.GetType(), m => m);
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
                _generationsTable.AddData(entity.Id, new EntityGeneration { generation = 0 });
                entity.generation = 0;
                ++_entityCount;
            }
            else
            {
                entity.Id = _freeEid.Pop();
                ref var generation = ref _generationsTable.GetData(entity.Id);
                generation.generation++;
                entity.generation = generation.generation;
            }

            _entitiesTable.AddData(entity.Id, entity);

            #if MODULES_DEBUG
            Logger.LogDebug($"Entity {id.ToString()} created", LogFilter.EntityLife);
            #endif

            OnEntityCreated?.Invoke(entity.Id);
            return entity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddComponent<T>(int eid, T component) where T : struct
        {
            var table = GetEscTable<T>();

            #if !MODULES_OPT
            if (table.Contains(eid))
                Logger.LogWarning($"Component {typeof(T).Name} exists in {eid.ToString()} entity and will be replaced");
            #endif

            table.AddData(eid, component);

            #if MODULES_DEBUG
            Logger.LogDebug($"Add to {eid.ToString()} {typeof(T).Name} component", LogFilter.EntityModifications);
            #endif

            OnEntityChanged?.Invoke(eid);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveComponent<T>(int eid) where T : struct
        {
            GetEscTable<T>().Remove(eid);
            #if MODULES_DEBUG
            Logger.LogDebug($"Remove from {eid.ToString()} {typeof(T).Name} component", LogFilter.EntityModifications);
            #endif
            OnEntityChanged?.Invoke(eid);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetComponent<T>(int id) where T : struct
        {
            return ref GetEscTable<T>().GetData(id);
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
        public EcsTable<T> GetEscTable<T>() where T : struct
        {
            CreateTableIfNeed<T>();
            return (EcsTable<T>)_data[typeof(T)];
        }

        public Span<T> GetRawData<T>() where T : struct
        {
            return GetEscTable<T>().GetRawData();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Query Select<T>() where T : struct
        {
            var table = GetEscTable<T>();
            return GetQuery(table);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Exist<T>() where T : struct
        {
            var table = GetEscTable<T>();
            var query = GetQuery(table);
            return query.Any();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySelectFirst<T>(out T c) where T : struct
        {
            var table = GetEscTable<T>();
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

        public bool HasComponent<T>(int id) where T : struct
        {
            return GetEscTable<T>().Contains(id);
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
                table.Remove(id);
            }

            _entitiesTable.Remove(id);
            _freeEid.Push(id);
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
        /// Init module: call Setup() and GetDependencies()
        /// You must activate module for IRunSystem, IRunPhysicSystem and IPostRunSystem
        /// </summary>
        /// <typeparam name="T">Type of module that you want to activate</typeparam>
        /// <seealso cref="ActivateModule{T}"/>
        /// <seealso cref="InitModule{T, T}"/>
        public void InitModule<T>(bool activateImmediately = false) where T : EcsModule
        {
            var module = GetModule<T>();
            #if MODULES_DEBUG
            if (module == null) throw new ModuleNotFoundException<T>();
            if (module.IsInitialized())
                throw new ModuleAlreadyInitializedException<T>();
            #endif
            module.Init(activateImmediately).Forget();
        }

        /// <summary>
        /// Initialize module: call Setup() and GetDependencies()
        /// You must activate module for IRunSystem, IRunPhysicSystem and IPostRunSystem
        /// ATTENTION! Only local modules can be parent. Dependency from global modules
        /// available in all systems by default
        /// </summary>
        /// <typeparam name="TModule">Type of module that you want to initialize</typeparam>
        /// <typeparam name="TParent">Parent module. TModule get dependencies from parent</typeparam>
        /// <seealso cref="InitModule{T}"/>
        /// <seealso cref="ActivateModule{T}"/>
        public void InitModule<TModule, TParent>(bool activateImmediately = false)
            where TModule : EcsModule
            where TParent : EcsModule
        {
            var module = GetModule<TModule>();
            var parent = GetModule<TParent>();
            #if MODULES_DEBUG
            if (module == null) throw new ModuleNotFoundException<TModule>();
            if (parent == null) throw new ModuleNotFoundException<TParent>();
            if (module.IsInitialized())
                throw new ModuleAlreadyInitializedException<TModule>();
            #endif
            module.Init(activateImmediately, parent).Forget();
        }

        /// <summary>
        /// Destroy module: calls Deactivate() in module and Destroy() in IDestroy systems
        /// </summary>
        /// <typeparam name="T">Type of module that you want to destroy</typeparam>
        public void DestroyModule<T>() where T : EcsModule
        {
            var module = GetModule<T>();
            #if MODULES_DEBUG
            if (module == null) throw new ModuleNotFoundException<T>();
            Logger.LogDebug($"Destroy module {typeof(T).Name}", LogFilter.ModulesFull);
            #endif
            module.Destroy();
        }

        /// <summary>
        /// Activate module: IRunSystem, IRunPhysicSystem and IPostRunSystem will start update
        /// </summary>
        /// <typeparam name="T">Type of module for activate</typeparam>
        /// <seealso cref="InitModule{T}"/>
        /// <seealso cref="DeactivateModule{T}"/>
        public void ActivateModule<T>() where T : EcsModule
        {
            var module = GetModule<T>();
            #if MODULES_DEBUG
            if (module == null) throw new ModuleNotFoundException<T>();
            Logger.LogDebug($"Activate module {typeof(T).Name}", LogFilter.ModulesFull);
            #endif
            module.SetActive(true);
        }

        /// <summary>
        /// Deactivate module: IRunSystem, IRunPhysicSystem and IPostRunSystem will stop update
        /// </summary>
        /// <typeparam name="T">Type of module for deactivate</typeparam>
        /// <seealso cref="DestroyModule{T}"/>
        /// <seealso cref="ActivateModule{T}"/>
        public void DeactivateModule<T>() where T : EcsModule
        {
            var module = GetModule<T>();
            #if MODULES_DEBUG
            if (module == null) throw new ModuleNotFoundException<T>();
            Logger.LogDebug($"Deactivate module {typeof(T).Name}", LogFilter.ModulesFull);
            #endif
            module.SetActive(false);
        }

        /// <summary>
        /// Old method for one frame entity. Use event systems instead. They makes special for
        /// event-based logic 
        /// </summary>
        /// <returns>New entity</returns>
        /// <seealso cref="EcsModule.GetSystemsOrder"/>
        [Obsolete("Use event systems instead")]
        public Entity CreateOneFrame()
        {
            return NewEntity().AddComponent(new EcsOneFrame());
        }

        public bool IsModuleActive<TModule>() where TModule : EcsModule
        {
            var localModule = GetModule<TModule>();
            return localModule is { IsActive: true };
        }

        public EcsModule? GetModule<T>() where T : EcsModule
        {
            if (_modules.TryGetValue(typeof(T), out var module))
                return module;
            return null;
        }

        private IEnumerable<EcsModule> CreateAllEcsModules()
        {
            var modules = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes()
                    .Where(t => t.IsSubclassOf(typeof(EcsModule)) && !t.IsAbstract)
                    .Select(t => (EcsModule)Activator.CreateInstance(t)));
            foreach (var module in modules)
            {
                module.InjectWorld(this);
                yield return module;
            }
        }

        public IEnumerable<EcsModule> GetAllModules()
        {
            return _modules.Values;
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
        public EcsOneData<T> GetOneData<T>() where T : struct
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
    }
}