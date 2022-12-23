using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ModulesFramework.Attributes;
using ModulesFramework.Data;
using ModulesFramework.DependencyInjection;
using ModulesFramework.Exceptions;
using ModulesFramework.Systems;

namespace ModulesFramework.Modules
{
    /// <summary>
    /// Base class for every module
    /// In modules you can create dependencies for your system and instantiate all prefabs that you need
    /// Don't create any entities in modules - use IInitSystem instead
    /// </summary>
    /// <seealso cref="IRunSystem"/>
    /// <seealso cref="GlobalModuleAttribute"/>
    public abstract class EcsModule
    {
        private readonly SortedDictionary<int, SystemsGroup> _systems = new SortedDictionary<int, SystemsGroup>();
        private SystemsGroup[] _systemsArr = Array.Empty<SystemsGroup>();
        private bool _isInit;
        private bool _isActive;
        private static readonly Dictionary<Type, object> _globalDependencies = new Dictionary<Type, object>();
        private static Exception? _exception;

        protected DataWorld world;

        private Type ConcreteType => GetType();
        public bool IsGlobal { get; }
        public bool IsActive => _isActive;

        protected EcsModule()
        {
            IsGlobal = ConcreteType.GetCustomAttribute<GlobalModuleAttribute>() != null;
        }

        internal void InjectWorld(DataWorld world)
        {
            this.world = world;
        }

        /// <summary>
        /// Activate concrete module: call and await EcsModule.Setup(), create all systems and insert dependencies
        /// </summary>
        /// <param name="world">The world where systems and entities will live</param>
        /// <param name="activateImmediately">Activate module after initialization?</param>
        /// <param name="parent">Parent module, when you need dependencies from other module</param>
        /// <seealso cref="Setup"/>
        public async Task Init(DataWorld world, bool activateImmediately = false, EcsModule? parent = null)
        {
            try
            {
                await Setup();

                UpdateGlobalDependencies();

                var systemOrder = GetSystemsOrder();
                foreach (var system in EcsUtilities.CreateSystems(ConcreteType))
                {
                    var order = 0;
                    if (systemOrder != null && systemOrder.ContainsKey(system.GetType()))
                        order = systemOrder[system.GetType()];

                    if (!_systems.ContainsKey(order))
                        _systems[order] = new SystemsGroup();

                    InsertDependencies(system, world, parent);
                    _systems[order].Add(system);
                }

                foreach (var p in _systems)
                {
                    p.Value.PreInit();
                    p.Value.Init();
                }

                _systemsArr = _systems.Values.ToArray();
                _isInit = true;
                if (activateImmediately)
                    SetActive(true);
            }
            catch (Exception e)
            {
                _exception = new Exception(e.Message, e);
                ExceptionsPool.AddException(_exception);
            }
        }

        /// <summary>
        /// Turn on/off the module.
        /// If false, IRunSystem, IRunPhysicSystem and IPostRunSystem will to be updated
        /// </summary>
        /// <param name="isActive">Flag to turn on/off the module</param>
        internal void SetActive(bool isActive)
        {
            if (!_isInit)
                throw new ModuleNotInitializedException(ConcreteType);
            if (isActive && !_isActive)
            {
                OnActivate();
                foreach (var p in _systems)
                    p.Value.Activate();
            }
            else if (_isActive)
            {
                OnDeactivate();
                foreach (var p in _systems)
                    p.Value.Deactivate();
            }

            _isActive = isActive;
        }

        /// <summary>
        /// Let you set order of systems. Default order is 0. Systems will be ordered by ascending 
        /// </summary>
        /// <returns>Dictionary with key - type of system and value - order</returns>
        protected virtual Dictionary<Type, int> GetSystemsOrder()
        {
            return null;
        }

        /// <summary>
        /// Return true if systems was create and init
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsInitialized()
        {
            return _isInit;
        }

        /// <summary>
        /// Just call RunPhysics at systems
        /// </summary>
        internal void RunPhysics()
        {
            if (!_isActive)
                return;

            CheckException();
            foreach (var p in _systemsArr)
            {
                p.RunPhysic();
            }
        }

        /// <summary>
        /// Just call Run at systems
        /// </summary>
        internal void Run()
        {
            if (!_isActive)
                return;

            CheckException();
            foreach (var p in _systemsArr)
            {
                p.Run();
            }
        }


        /// <summary>
        /// Just call RunLate at systems
        /// </summary>
        internal void PostRun()
        {
            if (!_isActive)
                return;

            CheckException();
            foreach (var p in _systemsArr)
            {
                p.PostRun();
            }
        }

        /// <summary>
        /// Calls before activate module and IActivateSystem
        /// </summary>
        public virtual void OnActivate()
        {
        }

        /// <summary>
        /// Calls before deactivate module and IDeactivateSystem
        /// When module destroy it calls OnDeactivate before
        /// If module was inactive, it will not be called
        /// </summary>
        public virtual void OnDeactivate()
        {
        }

        /// <summary>
        /// Calls before destroy systems in the module
        /// You can clear something here, like release some resources
        /// </summary>
        public virtual void OnDestroy()
        {
        }

        private void DestroySystems()
        {
            foreach (var p in _systems)
            {
                p.Value.Destroy();
            }

            _systems.Clear();
        }

        /// <summary>
        /// For internal usage only
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Destroy()
        {
            if (!_isInit) return;
            SetActive(false);
            OnDestroy();
            DestroySystems();
            _isInit = false;
        }

        /// <summary>
        /// Call when module activate
        /// You can create here all dependencies and game objects, that you need
        /// </summary>
        protected virtual async Task Setup()
        {
            await Task.CompletedTask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckException()
        {
            if (_exception == null)
                return;
            throw _exception;
        }

        private void UpdateGlobalDependencies()
        {
            if (!IsGlobal)
                return;

            foreach (var kvp in GetDependencies())
            {
                if (_globalDependencies.ContainsKey(kvp.Key))
                    continue;
                _globalDependencies.Add(kvp.Key, kvp.Value);
            }
        }

        private void InsertDependencies(ISystem system, DataWorld world, EcsModule? parent = null)
        {
            var dependencies = GetDependencies();
            var parentDependencies = parent?.GetDependencies();
            var setupMethod = GetSetupMethod(system);
            if (setupMethod != null)
            {
                var parameters = setupMethod.GetParameters();
                var injections = new object[parameters.Length];
                var i = 0;
                foreach (var parameter in parameters)
                {
                    var t = parameter.ParameterType;
                    if (t == typeof(DataWorld))
                    {
                        injections[i++] = world;
                        continue;
                    }

                    if (_globalDependencies.ContainsKey(t))
                    {
                        injections[i++] = _globalDependencies[t];
                        continue;
                    }

                    if (dependencies.ContainsKey(t))
                    {
                        injections[i++] = dependencies[t];
                        continue;
                    }

                    if (parentDependencies != null && parentDependencies.ContainsKey(t))
                    {
                        injections[i++] = parentDependencies[t];
                        continue;
                    }

                    if (t.BaseType == typeof(OneData))
                    {
                        var data = world.GetOneData(t);
                        if (data == null)
                            ThrowOneDataException(t);
                        injections[i++] = data;
                        continue;
                    }

                    throw new Exception($"Can't find injection {parameter.ParameterType} in method {setupMethod.Name}");
                }

                setupMethod.Invoke(system, injections);
                return;
            }

            var fields = system.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var field in fields)
            {
                var t = field.FieldType;
                if (t == typeof(DataWorld))
                {
                    field.SetValue(system, world);
                    continue;
                }

                if (_globalDependencies.ContainsKey(t))
                    field.SetValue(system, _globalDependencies[t]);
                if (dependencies.ContainsKey(t))
                    field.SetValue(system, dependencies[t]);
                if (parentDependencies != null && parentDependencies.ContainsKey(t))
                    field.SetValue(system, parentDependencies[t]);

                if (t.BaseType == typeof(OneData))
                {
                    InsertOneData(t, system, field);
                }
            }
        }

        private void InsertOneData(Type t, ISystem system, FieldInfo field)
        {
            var oneData = world.GetOneData(t);

            if (oneData == null)
                ThrowOneDataException(t);

            field.SetValue(system, oneData);
        }

        private void ThrowOneDataException(Type t)
        {
            throw new ApplicationException(
                $"Type {t.GetGenericArguments()[0]} does not exist. Are you forget to add it in {GetType().Name} module?");
        }

        private MethodInfo GetSetupMethod(ISystem system)
        {
            var methods = system.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance);
            foreach (var methodInfo in methods)
            {
                if (methodInfo.GetCustomAttribute<SetupAttribute>() == null)
                    continue;
                return methodInfo;
            }

            return null;
        }

        /// <summary>
        /// Must return dictionary of dependencies for all systems in the module
        /// Dependencies in systems MUST BE private and non-static
        /// </summary>
        public virtual Dictionary<Type, object> GetDependencies()
        {
            return new Dictionary<Type, object>(0);
        }

        /// <summary>
        /// Let you get global dependencies in local module.
        /// It can be useful when you create some local service that needs global dependency.
        /// </summary>
        /// <typeparam name="TModule">Module from where you need dependency</typeparam>
        /// <typeparam name="TDependency">Dependency type</typeparam>
        /// <returns>Dependency or null</returns>
        protected TDependency? GetGlobalDependency<TModule, TDependency>()
            where TModule : EcsModule where TDependency : class
        {
            var module = world.GetModule<TModule>();
            if (module == null)
                return null;

            var dependencies = module.GetDependencies();
            if (dependencies.ContainsKey(typeof(TDependency)))
                return dependencies[typeof(TDependency)] as TDependency;
            return null;
        }
    }
}