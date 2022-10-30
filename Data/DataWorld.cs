using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ModulesFramework.Modules;
using ModulesFramework.Modules.Components;

namespace ModulesFramework.Data
{
    public partial class DataWorld
    {
        private readonly Dictionary<Type, EcsTable> _data = new Dictionary<Type, EcsTable>();
        private readonly EcsTable<Entity> _entitiesTable = new EcsTable<Entity>();
        private int _entityCount;
        private readonly Stack<int> _freeEid = new Stack<int>();
        private readonly ModulesRepository _modules;

        public DataWorld(ModulesRepository modulesRepository)
        {
            _modules = modulesRepository;
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
            return entity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddComponent<T>(int eid, T component) where T : struct
        {
            GetEscTable<T>().AddData(eid, component);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveComponent<T>(int id) where T : struct
        {
            GetEscTable<T>().Remove(id);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetComponent<T>(int id) where T : struct
        {
            return ref GetEscTable<T>().GetData(id);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int[] GetLinearData<T>() where T : struct
        {
            return GetEscTable<T>().GetEntitiesId();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsTable<T> GetEscTable<T>() where T : struct
        {
            CreateTableIfNeed<T>();
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DestroyEntity(int id)
        {
            foreach (var table in _data.Values)
            {
                table.Remove(id);
            }

            _entitiesTable.Remove(id);
            _freeEid.Push(id);
        }

        /// <summary>
        /// Init module: call Setup() and GetDependencies()
        /// You must activate module for IRunSystem, IRunPhysicSystem and IPostRunSystem
        /// </summary>
        /// <typeparam name="T">Type of module that you want to activate</typeparam>
        /// <seealso cref="ActivateModule{T}"/>
        /// <seealso cref="InitModule{T, T}"/>
        public void InitModule<T>() where T : EcsModule
        {
            NewEntity()
                .AddComponent(new ModuleInitSignal
                {
                    moduleType = typeof(T)
                });
        }

        /// <summary>
        /// Initialize module: call Setup() and GetDependencies()
        /// You must activate module for IRunSystem, IRunPhysicSystem and IPostRunSystem
        /// </summary>
        /// <typeparam name="TModule">Type of module that you want to initialize</typeparam>
        /// <typeparam name="TParent">Parent module. TModule get dependencies from parent</typeparam>
        /// <seealso cref="InitModule{T}"/>
        /// <seealso cref="ActivateModule{T}"/>
        public void InitModule<TModule, TParent>()
            where TModule : EcsModule
            where TParent : EcsModule
        {
            NewEntity()
                .AddComponent(new ModuleInitSignal
                {
                    moduleType = typeof(TModule),
                    dependenciesModule = typeof(TParent)
                });
        }

        /// <summary>
        /// Destroy module: calls Deactivate() in module and Destroy() in IDestroy systems
        /// </summary>
        /// <typeparam name="T">Type of module that you want to destroy</typeparam>
        public void DestroyModule<T>() where T : EcsModule
        {
            NewEntity().AddComponent(new ModuleDestroySignal { ModuleType = typeof(T) });
        }

        /// <summary>
        /// Activate module: IRunSystem, IRunPhysicSystem and IPostRunSystem will start update
        /// </summary>
        /// <typeparam name="T">Type of module for activate</typeparam>
        /// <seealso cref="InitModule{T}"/>
        /// <seealso cref="DeactivateModule{T}"/>
        public void ActivateModule<T>() where T : EcsModule
        {
            NewEntity()
                .AddComponent(new ModuleChangeStateSignal
                {
                    state = true,
                    moduleType = typeof(T)
                });
        }

        /// <summary>
        /// Deactivate module: IRunSystem, IRunPhysicSystem and IPostRunSystem will stop update
        /// </summary>
        /// <typeparam name="T">Type of module for deactivate</typeparam>
        /// <seealso cref="DestroyModule{T}"/>
        /// <seealso cref="ActivateModule{T}"/>
        public void DeactivateModule<T>() where T : EcsModule
        {
            NewEntity()
                .AddComponent(new ModuleChangeStateSignal
                {
                    state = false,
                    moduleType = typeof(T)
                });
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
            return _modules.IsModuleActive<TModule>();
        }
    }
}