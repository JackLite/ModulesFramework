using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using ModulesFramework.Modules;

namespace ModulesFramework.Data
{
    public partial class DataWorld
    {
        private readonly Dictionary<Type, EcsTable> _data = new Dictionary<Type, EcsTable>();
        private readonly EcsTable<Entity> _entitiesTable = new EcsTable<Entity>();
        private int _entityCount;
        private readonly Stack<int> _freeEid = new Stack<int>(64);
        private readonly Dictionary<Type, EcsModule> _modules;
        private Dictionary<Type, OneData> _oneDatas = new Dictionary<Type, OneData>();

        public event Action<int> OnEntityCreated; 
        public event Action<int> OnEntityChanged; 
        public event Action<int> OnEntityDestroyed;

        internal event Action<Type, OneData> OnOneDataCreated;

        public DataWorld()
        {
            _modules = CreateAllEcsModules().ToDictionary(m => m.GetType(), m => m);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Entity NewEntity()
        {
            int id;
            if (_freeEid.Count == 0)
            {
                ++_entityCount;
                id = _entityCount;
            }
            else
            {
                id = _freeEid.Pop();
            }

            var entity = new Entity
            {
                Id = id,
                World = this
            };
            _entitiesTable.AddData(id, entity);
            OnEntityCreated?.Invoke(id);
            return entity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddComponent<T>(int eid, T component) where T : struct
        {
            GetEscTable<T>().AddData(eid, component);
            OnEntityChanged?.Invoke(eid);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveComponent<T>(int eid) where T : struct
        {
            GetEscTable<T>().Remove(eid);
            OnEntityChanged?.Invoke(eid);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetComponent<T>(int id) where T : struct
        {
            return ref GetEscTable<T>().GetData(id);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int[] GetLinearData<T>() where T : struct
        {
            return GetEscTable<T>().GetEntitiesId();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsTable<T> GetEscTable<T>() where T : struct
        {
            #if !MODULES_FAST
            CreateTableIfNeed<T>();
            #endif
            return (EcsTable<T>)_data[typeof(T)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Query<T> Select<T>() where T : struct
        {
            var table = GetEscTable<T>();
            var query = new Query<T>(this, table);
            return query;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Exist<T>() where T : struct
        {
            var table = GetEscTable<T>();
            var query = new Query<T>(this, table);
            return query.Any();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySelectFirst<T>(out T c) where T : struct
        {
            var table = GetEscTable<T>();
            var query = new Query<T>(this, table);
            return query.TrySelectFirst<T>(out c);
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
            #endif
            module.Init(this, activateImmediately).Forget();
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
            #endif
            module.Init(this, activateImmediately, parent).Forget();
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
            #endif
            module.SetActive(false);
        }

        /// <summary>
        /// Allow to create one frame entity. That entity will be destroyed after all run systems processed (include IEcsRunLate)
        /// WARNING: one frame creates immediately, but if some systems processed BEFORE creation one frame entity
        /// they WILL NOT processed that entity. You can create one frame in RunSystem and processed them in RunLateSystem.
        /// Also you can use GetSystemOrder() in your module for setting order of systems.
        /// </summary>
        /// <returns>New entity</returns>
        /// <seealso cref="EcsModule.GetSystemsOrder"/>
        public Entity CreateOneFrame()
        {
            return NewEntity().AddComponent(new EcsOneFrame());
        }

        public bool IsModuleActive<TModule>() where TModule : EcsModule
        {
            var localModule = GetModule<TModule>();
            return localModule is { IsActive: true };
        }

        internal EcsModule? GetModule<T>() where T : EcsModule
        {
            if (_modules.TryGetValue(typeof(T), out var module))
                return module;
            return null;
        }

#nullable disable
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
#nullable restore
        public IEnumerable<EcsModule> GetAllModules()
        {
            return _modules.Select(kvp => kvp.Value);
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