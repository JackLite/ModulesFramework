#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ModulesFramework.Attributes;
using ModulesFramework.DependencyInjection;
using ModulesFramework.Exceptions;
using ModulesFramework.Systems;
using DataWorld = ModulesFramework.Data.DataWorld;

namespace ModulesFramework.Modules
{
    /// <summary>
    /// Base class for every module
    /// In modules you can create dependencies for your system and instantiate all prefabs that you need
    /// Don't create any entities in modules - use IPreInitSystem instead
    /// </summary>
    /// <seealso cref="IRunSystem"/>
    /// <seealso cref="GlobalModuleAttribute"/>
    public abstract partial class EcsModule
    {
        private readonly SortedDictionary<int, SystemsGroup> _systems = new SortedDictionary<int, SystemsGroup>();
        private SystemsGroup[] _systemsArr = Array.Empty<SystemsGroup>();
        private static readonly List<EcsModule> _globalModules = new List<EcsModule>();
        private List<ISystem>? _createdSystem;

        protected DataWorld world = null!;

        private Type ConcreteType => GetType();

        public bool IsGlobal { get; }
        public bool IsInitialized { get; private set; }

        /// <summary>
        ///     Used for events so we can rise event in Activate
        /// </summary>
        internal bool IsActivating { get; private set; }

        public bool IsActive { get; private set; }

        public virtual IEnumerable<Type> ComposedOf => Array.Empty<Type>();
        public bool IsSubmodule { get; private set; }
        public bool IsComposed { get; internal set; }
        public bool IsRoot => !IsSubmodule && !IsComposed;
        public bool IsInitWithParent { get; private set; }
        public bool IsActiveWithParent { get; private set; }
        public EcsModule? Parent { get; private set; }

        internal IEnumerable<Type> Systems => _systemsArr.SelectMany(g => g.AllSystems).Distinct();

        public event Action? OnInitialized;
        public event Action? OnActivated;
        public event Action? OnDeactivated;
        public event Action? OnDestroyed;

        protected EcsModule()
        {
            IsGlobal = ConcreteType.GetCustomAttribute<GlobalModuleAttribute>() != null;
        }

        internal void InjectWorld(DataWorld dataWorld)
        {
            world = dataWorld;
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

            await SetupComposition();

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

        private void UpdateGlobalDependencies()
        {
            if (!IsGlobal)
                return;

            _globalModules.Add(this);
        }

        private void ProcessSystems()
        {
            foreach (var submodule in _composedModules)
            {
                submodule.ProcessSystems();
            }

            if (_createdSystem == null)
                CreateSystems();

            InitSystems();
            foreach (var submodules in _submodules.Values)
            {
                foreach (var submodule in submodules)
                {
                    if (submodule.IsInitWithParent)
                        submodule.ProcessSystems();
                }
            }

            IsInitialized = true;
#if MODULES_DEBUG
            world.Logger.LogDebug($"Call OnInit in {GetType().Name}", LogFilter.ModulesFull);
#endif
            OnInit();
            OnInitialized?.Invoke();
        }

        private void CreateSystems()
        {
            var systemOrder = GetSystemsOrder();
            _createdSystem = GetSystems().ToList();
            foreach (var system in _createdSystem)
            {
                var order = 0;
                if (systemOrder.ContainsKey(system.GetType()))
                    order = systemOrder[system.GetType()];

                if (!_systems.ContainsKey(order))
                    _systems[order] = new SystemsGroup();

                _systems[order].Add(system);
            }
        }

        protected virtual IEnumerable<ISystem> GetSystems()
        {
            return world.GetSystems(ConcreteType);
        }

        internal void InitSystems()
        {
#if MODULES_DEBUG
            world.Logger.LogDebug($"Module {GetType().Name} systems pre-init", LogFilter.SystemsInit);
#endif

            foreach (var system in _createdSystem)
                InsertDependencies(system, world);

            foreach (var p in _systems)
                p.Value.PreInit(world);

#if MODULES_DEBUG
            world.Logger.LogDebug($"Module {GetType().Name} systems init", LogFilter.SystemsInit);
#endif

            foreach (var p in _systems)
            {
                p.Value.Init(world);
                foreach (var subscriptionType in p.Value.SubscriptionTypes)
                {
                    RegisterSubscriber(subscriptionType, p.Value, p.Key, true);
                }
            }

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

            IsActivating = isActive;
            if (isActive && !IsActive)
            {
                SetActiveComposition(true);
                Activate();
                SetSubmodulesActive(true);
                OnActivate();
                OnActivated?.Invoke();
            }
            else if (!isActive && IsActive)
            {
                SetActiveComposition(false);
                SetSubmodulesActive(false);
                Deactivate();
                OnDeactivate();
                OnDeactivated?.Invoke();
            }

            IsActive = isActive;
        }

        private void Activate()
        {
#if MODULES_DEBUG
            world.Logger.LogDebug($"Activate systems in {GetType().Name}", LogFilter.SystemsInit);
#endif
            foreach (var p in _systems)
            {
                foreach (var eventType in p.Value.EventTypes)
                    RegisterListener(eventType, p.Value);

                foreach (var eventType in p.Value.SubscriptionTypes)
                    RegisterSubscriber(eventType, p.Value, p.Key);

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

            _runEvents.Clear();
            _postRunEvents.Clear();
            _frameEndEvents.Clear();
            foreach (var p in _systems)
            {
                p.Value.Deactivate(world);
                foreach (var eventType in p.Value.EventTypes)
                    UnregisterListener(eventType, p.Value);

                foreach (var eventType in p.Value.SubscriptionTypes)
                    UnregisterSubscriber(eventType, p.Value);
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

            foreach (var module in _composedModules)
            {
                module.Run();
            }

            foreach (var p in _systemsArr)
            {
                foreach (var eventType in p.EventTypes)
                    RunEvents(eventType);

                p.Run(world);
            }

            foreach (var submodules in _submodules.Values)
            {
                foreach (var submodule in submodules)
                {
                    submodule.Run();
                }
            }
        }

        /// <summary>
        /// Just call RunPhysics at systems
        /// </summary>
        internal void RunPhysics()
        {
            if (!IsActive)
                return;

            foreach (var module in _composedModules)
            {
                module.RunPhysics();
            }

            foreach (var p in _systemsArr)
            {
                p.RunPhysic(world);
            }

            foreach (var submodules in _submodules.Values)
            {
                foreach (var submodule in submodules)
                {
                    submodule.RunPhysics();
                }
            }
        }

        /// <summary>
        /// Just call RunLate at systems
        /// </summary>
        internal void PostRun()
        {
            if (!IsActive)
                return;

            foreach (var module in _composedModules)
            {
                module.PostRun();
            }

            foreach (var p in _systemsArr)
            {
                foreach (var eventType in p.EventTypes)
                    PostRunEvents(eventType);

                p.PostRun(world);
            }

            foreach (var submodules in _submodules.Values)
            {
                foreach (var submodule in submodules)
                {
                    submodule.PostRun();
                }
            }
        }

        internal void FrameEnd()
        {
            if (!IsActive)
                return;

            foreach (var module in _composedModules)
            {
                module.FrameEnd();
            }

            foreach (var p in _systemsArr)
            {
                foreach (var eventType in p.EventTypes)
                    FrameEndEvents(eventType);
            }

            foreach (var submodules in _submodules.Values)
            {
                foreach (var submodule in submodules)
                {
                    submodule.FrameEnd();
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

                foreach (var subscriptionType in p.Value.SubscriptionTypes)
                {
                    UnregisterSubscriber(subscriptionType, p.Value, true);
                }
            }

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
            foreach (var submodules in _submodules.Values)
            {
                foreach (var submodule in submodules)
                {
                    if (submodule.IsActive)
                        submodule.SetActive(false);
                }
            }

            SetActive(false);

            // any submodule must be destroyed with parent cause it has dependencies from it that may being destroyed
            foreach (var submodules in _submodules.Values)
            {
                foreach (var submodule in submodules)
                {
                    if (submodule.IsInitialized)
                        submodule.Destroy();
                }
            }

            OnDestroy();
            DestroySystems();

            foreach (var composedModule in _composedModules)
            {
                composedModule.Destroy();
            }

            IsInitialized = false;
            OnDestroyed?.Invoke();
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
                    {
                        throw new Exception(
                            $"Can't find injection {parameter.ParameterType} in method {setupMethod.Name}");
                    }

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
                dependency = Parent.GetDependency(type);
                if (dependency != null)
                    return dependency;
            }

            foreach (var composedModule in _composedModules)
            {
                dependency = composedModule.GetDependency(type);
                if (dependency != null)
                    return dependency;
            }

            return world.GetGlobalDependency(type);
        }

        /// <summary>
        /// Let you set order of systems. Default order is 0. Systems will be ordered by ascending
        /// </summary>
        /// <returns>Dictionary with key - type of system and value - order</returns>
        protected virtual Dictionary<Type, int> GetSystemsOrder()
        {
            return new Dictionary<Type, int>(0);
        }
    }
}