#nullable enable
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
using ModulesFramework.Systems.Events;
using DataWorld = ModulesFramework.Data.World.DataWorld;

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
        private static readonly List<EcsModule> _globalModules = new List<EcsModule>();

#nullable disable
        protected DataWorld world;
#nullable enable

        private Type ConcreteType => GetType();

        /// <summary>
        ///     Set that marks to what world belongs this module.
        ///     Be careful cause all systems in module will run once per world
        ///     Note: probably you will never need this, but for some complex multiplayer games it will be
        ///     necessary in host mode
        /// </summary>
        public virtual HashSet<int> WorldIndex =>
            new HashSet<int>
            {
                0
            };

        public bool IsGlobal { get; }
        public bool IsInitialized { get; private set; }
        public bool IsActive { get; private set; }

        public bool IsSubmodule { get; private set; }
        public bool IsInitWithParent { get; private set; }
        public bool IsActiveWithParent { get; private set; }
        public EcsModule? Parent { get; private set; }

        internal IEnumerable<Type> Systems => _systemsArr.SelectMany(g => g.AllSystems).Distinct();

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
        /// <param name="activateImmediately">Activate module after initialization?</param>
        /// <seealso cref="Setup"/>
        public async Task Init(bool activateImmediately = false)
        {
            try
            {
                await StartInit();

                ProcessSystems();
                if (activateImmediately)
                    SetActive(true);
            }
            catch (Exception e)
            {
                world.Logger.RethrowException(e);
            }
        }

        private async Task StartInit()
        {
            #if MODULES_DEBUG
            world.Logger.LogDebug($"Start init module {GetType().Name}", LogFilter.ModulesFull);
            #endif

            await Setup();

            #if MODULES_DEBUG
            world.Logger.LogDebug($"Module {GetType().Name} setup is done", LogFilter.ModulesFull);
            #endif

            UpdateGlobalDependencies();
            await SetupSubmodules();

            await OnSetupEnd();
        }

        /// <summary>
        /// Call when module activate
        /// You can create here all dependencies and game objects, that you need
        /// </summary>
        protected virtual async Task Setup()
        {
            await Task.CompletedTask;
        }

        private async Task SetupSubmodules()
        {
            var tasks = new List<Task>();
            foreach (var submodule in world.GetSubmodules(ConcreteType))
            {
                if (submodule.IsInitWithParent)
                    tasks.Add(submodule.StartInit());
            }

            await Task.WhenAll(tasks);
        }

        private void UpdateGlobalDependencies()
        {
            if (!IsGlobal)
                return;

            _globalModules.Add(this);
        }

        private void ProcessSystems()
        {
            CreateSystems();
            InitSystems();
            foreach (var submodule in world.GetSubmodules(ConcreteType))
            {
                if (submodule.IsInitWithParent)
                    submodule.ProcessSystems();
            }

            IsInitialized = true;
            #if MODULES_DEBUG
            world.Logger.LogDebug($"Call OnInit in {GetType().Name}", LogFilter.ModulesFull);
            #endif
            OnInit();
        }

        private void CreateSystems()
        {
            var systemOrder = GetSystemsOrder();
            foreach (var system in GetSystems())
            {
                var order = 0;
                if (systemOrder.ContainsKey(system.GetType()))
                    order = systemOrder[system.GetType()];

                if (!_systems.ContainsKey(order))
                    _systems[order] = new SystemsGroup();

                InsertDependencies(system, world);
                _systems[order].Add(system);
            }
        }

        protected virtual IEnumerable<ISystem> GetSystems()
        {
            return EcsUtilities.CreateSystems(ConcreteType);
        }

        internal void InitSystems()
        {
            #if MODULES_DEBUG
            world.Logger.LogDebug($"Module {GetType().Name} systems preinit", LogFilter.SystemsInit);
            #endif

            foreach (var p in _systems)
                p.Value.PreInit(world);

            #if MODULES_DEBUG
            world.Logger.LogDebug($"Module {GetType().Name} systems init", LogFilter.SystemsInit);
            #endif

            foreach (var p in _systems)
                p.Value.Init(world);

            _systemsArr = _systems.Values.ToArray();
        }

        /// <summary>
        /// Turn on/off the module.
        /// If false, IRunSystem, IRunPhysicSystem and IPostRunSystem will to be updated
        /// </summary>
        /// <param name="isActive">Flag to turn on/off the module</param>
        internal void SetActive(bool isActive)
        {
            #if MODULES_DEBUG
            var logMsgStart = isActive ? "activate" : "deactivate";
            world.Logger.LogDebug($"Start {logMsgStart} module {GetType().Name}", LogFilter.ModulesFull);
            #endif

            if (!IsInitialized)
                throw new ModuleNotInitializedException(ConcreteType);

            if (isActive && !IsActive)
            {
                Activate();
                SetSubmodulesActive(true);
                OnActivate();
            }
            else if (!isActive && IsActive)
            {
                SetSubmodulesActive(false);
                Deactivate();
                OnDeactivate();
            }

            IsActive = isActive;
        }

        private void SetSubmodulesActive(bool isActive)
        {
            foreach (var submodule in world.GetSubmodules(ConcreteType))
            {
                if (submodule.IsActiveWithParent)
                    submodule.SetActive(isActive);
            }
        }

        private void Activate()
        {
            #if MODULES_DEBUG
            world.Logger.LogDebug($"Activate systems in {GetType().Name}", LogFilter.SystemsInit);
            #endif
            foreach (var p in _systems)
            {
                foreach (var eventType in p.Value.EventTypes)
                {
                    world.RegisterListener(eventType, p.Value);
                }

                p.Value.Activate(world);
            }
            #if MODULES_DEBUG
            world.Logger.LogDebug($"Call OnActivate in {GetType().Name}", LogFilter.ModulesFull);
            #endif
        }

        private void Deactivate()
        {
            #if MODULES_DEBUG
            world.Logger.LogDebug($"Deactivate systems in {GetType().Name}", LogFilter.SystemsDestroy);
            #endif

            foreach (var p in _systems)
            {
                p.Value.Deactivate(world);
                foreach (var eventType in p.Value.EventTypes)
                {
                    world.UnregisterListener(eventType, p.Value);
                }
            }
            #if MODULES_DEBUG
            world.Logger.LogDebug($"Call OnDeactivate in {GetType().Name}", LogFilter.ModulesFull);
            #endif
        }

        /// <summary>
        /// Just call Run at systems
        /// </summary>
        internal void Run()
        {
            if (!IsActive)
                return;

            foreach (var p in _systemsArr)
            {
                foreach (var eventType in p.EventTypes)
                {
                    var handler = world.GetHandlers(eventType);
                    try
                    {
                        handler.Run<IRunEventSystem>();
                    }
                    catch (Exception e)
                    {
                        world.Logger.RethrowException(e);
                    }
                }

                p.Run(world);
            }
        }

        /// <summary>
        /// Just call RunPhysics at systems
        /// </summary>
        internal void RunPhysics()
        {
            if (!IsActive)
                return;

            foreach (var p in _systemsArr)
            {
                p.RunPhysic(world);
            }
        }


        /// <summary>
        /// Just call RunLate at systems
        /// </summary>
        internal void PostRun()
        {
            if (!IsActive)
                return;

            foreach (var p in _systemsArr)
            {
                foreach (var eventType in p.EventTypes)
                {
                    var handler = world.GetHandlers(eventType);
                    handler.Run<IPostRunEventSystem>();
                }

                p.PostRun(world);
            }

            foreach (var p in _systemsArr)
            {
                foreach (var eventType in p.EventTypes)
                {
                    var handler = world.GetHandlers(eventType);
                    handler.Run<IFrameEndEventSystem>();
                }
            }
        }

        /// <summary>
        /// Calls after setup finished and before IPreInit and IInit
        /// </summary>
        public virtual Task OnSetupEnd()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Calls after all <see cref="IPreInitSystem"/> and <see cref="IInitSystem"/> proceed
        /// </summary>
        public virtual void OnInit()
        {
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
            #if MODULES_DEBUG
            world.Logger.LogDebug($"Destroy systems in {GetType().Name}", LogFilter.SystemsDestroy);
            #endif

            foreach (var p in _systems)
            {
                p.Value.Destroy(world);
            }

            _systems.Clear();
            IsInitialized = false;
        }

        /// <summary>
        /// For internal usage only
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Destroy()
        {
            if (!IsInitialized)
            {
                world.Logger.LogWarning($"Destroy module {GetType().Name} that not initialized");
                return;
            }

            #if MODULES_DEBUG
            world.Logger.LogDebug($"Start destroy module {GetType().Name}", LogFilter.ModulesFull);
            #endif

            // even if module was manually activate it still must be deactivated when parent module destroyed
            foreach (var submodule in world.GetSubmodules(ConcreteType))
            {
                if (submodule.IsActive)
                    submodule.SetActive(false);
            }

            SetActive(false);

            // any submodule must be destroyed with parent cause it has dependencies from it that may being destroyed
            foreach (var submodule in world.GetSubmodules(ConcreteType))
            {
                submodule.Destroy();
            }

            OnDestroy();
            DestroySystems();
            IsInitialized = false;
        }

        private void InsertDependencies(ISystem system, DataWorld world)
        {
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

                    if (t.BaseType == typeof(OneData))
                    {
                        var data = world.GetOneData(t);
                        if (data == null)
                            ThrowOneDataException(t);
                        else
                            injections[i++] = data;
                        continue;
                    }

                    object? dependency = null;
                    foreach (var module in _globalModules)
                    {
                        dependency = module.GetDependency(t);
                        if (dependency != null)
                            break;
                    }

                    if (dependency == null)
                    {
                        dependency = GetDependency(t);
                    }

                    if (dependency == null)
                        throw new Exception(
                            $"Can't find injection {parameter.ParameterType} in method {setupMethod.Name}");

                    injections[i++] = dependency;
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

                if (t.BaseType == typeof(OneData))
                {
                    var data = world.GetOneData(t);
                    if (data == null)
                        ThrowOneDataException(t);
                    else
                        field.SetValue(system, data);
                    continue;
                }

                object? dependency = null;
                foreach (var module in _globalModules)
                {
                    dependency = module.GetDependency(t);
                    if (dependency != null)
                        break;
                }

                if (dependency == null)
                {
                    dependency = GetDependency(t);
                }

                if (dependency != null)
                    field.SetValue(system, dependency);
                else
                    world.Logger.LogDebug(
                        $"Can't inject dependency for {field.Name}. Ignore this message if you create field by yourself",
                        LogFilter.ModulesFull
                    );
            }
        }

        private void ThrowOneDataException(Type t)
        {
            throw new ApplicationException(
                $"Type {t.GetGenericArguments()[0]} does not exist. You should use {nameof(DataWorld.OneData)}");
        }

        private MethodInfo? GetSetupMethod(ISystem system)
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
        /// Return dependency from module by type
        /// </summary>
        /// <typeparam name="T">Type of dependency</typeparam>
        /// <returns>Dependency or null if object not exists</returns>
        public T? GetDependency<T>() where T : class
        {
            return GetDependency(typeof(T)) as T;
        }

        /// <summary>
        /// Return dependency from module by type
        /// This method should be override by user's modules
        /// You can use any IoC for that
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public virtual object? GetDependency(Type type)
        {
            if (GetDependencies().TryGetValue(type, out var dependency))
                return dependency;

            if (IsSubmodule)
            {
                return Parent.GetDependency(type);
            }

            return null;
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

            return module?.GetDependency<TDependency>();
        }

        /// <summary>
        ///     Mark module as submodule of parent
        /// </summary>
        /// <param name="parent">Parent module</param>
        /// <param name="initWithParent">Mark that module must initialized with parent</param>
        /// <param name="activeWithParent">Mark that module must activated with parent</param>
        internal void MarkSubmodule(EcsModule parent, bool initWithParent, bool activeWithParent)
        {
            IsSubmodule = true;
            Parent = parent;
            IsInitWithParent = initWithParent;
            IsActiveWithParent = activeWithParent;
        }

        /// <summary>
        /// Let you set order of systems. Default order is 0. Systems will be ordered by ascending 
        /// </summary>
        /// <returns>Dictionary with key - type of system and value - order</returns>
        protected virtual Dictionary<Type, int> GetSystemsOrder()
        {
            return new Dictionary<Type, int>();
        }
    }
}