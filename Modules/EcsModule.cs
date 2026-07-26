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
using ModulesFramework.Utils.Types;
using DataWorld = ModulesFramework.Data.DataWorld;

namespace ModulesFramework.Modules
{
    /// <summary>
    /// Base class for every module
    /// In modules you can create dependencies for your system
    /// Don't create any entities in modules - use IPreInitSystem instead. It's an advice, not a rule.
    /// </summary>
    /// <seealso cref="GlobalModuleAttribute"/>
    /// <seealso cref="SubmoduleAttribute"/>
    public abstract partial class EcsModule
    {
        private static readonly List<EcsModule> _globalModules = new List<EcsModule>();
        private bool _isSetup;

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
        public virtual bool IsInitWithParent { get; protected set; }
        public virtual bool IsActiveWithParent { get; protected set; }
        public EcsModule? Parent { get; private set; }

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
                _systemTypes = SystemTypes;
                // setup self and submodules
                await SetupSelfAndSubmodules();
                // process all dependencies
                InsertDependencies();
                // call OnSetupEnd self and submodules
                await OnSetupEndSelfAndSubmodules();

                #if MODULES_DEBUG
                RegisterWatchers();
                #endif

                ProcessSystems();
                if (activateImmediately)
                    SetActive(true);
            }
            catch (Exception e)
            {
                world.Logger.RethrowException(e);
            }
        }

        private async Task SetupSelfAndSubmodules()
        {
            #if MODULES_DEBUG
            world.Logger.LogDebug($"Start init module {GetType().GetTypeName()}", LogFilter.ModulesFull);
#endif

            await SetupComposition();

            await Setup();

#if MODULES_DEBUG
            world.Logger.LogDebug($"Module {GetType().GetTypeName()} setup is done", LogFilter.ModulesFull);
#endif

            UpdateGlobalDependencies();

            CreateSystemsGroup();

            #if MODULES_DEBUG
            CreateWatchers();
            #endif

            await SetupSubmodules();
        }

        private void InsertDependencies()
        {
            foreach (var system in _createdSystem!)
                InsertDependencies(system);

            #if MODULES_DEBUG
            foreach (var watcher in _componentWatchers!)
                InsertDependencies(watcher);
            #endif

            foreach (var submodule in Submodules)
            {
                if (submodule.IsInitWithParent)
                    submodule.InsertDependencies();
            }
        }

        private async Task OnSetupEndSelfAndSubmodules()
        {
            foreach (var group in _submodulesGroups)
            {
                var tasks = new List<Task>();
                foreach (var submodule in group.modules)
                {
                    if (submodule.IsInitWithParent)
                        tasks.Add(submodule.OnSetupEndSelfAndSubmodules());
                }

                await Task.WhenAll(tasks);
            }

            _isSetup = true;

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

            ProcessInitSystems();
            foreach (var group in _submodulesGroups)
            {
                foreach (var submodule in group.modules)
                {
                    if (submodule.IsInitWithParent)
                        submodule.ProcessSystems();
                }
            }

            IsInitialized = true;
#if MODULES_DEBUG
            world.Logger.LogDebug($"Call OnInit in {GetType().GetTypeName()}", LogFilter.ModulesFull);
#endif
            OnInit();
            OnInitialized?.Invoke();
        }

        internal void ProcessInitSystems()
        {
#if MODULES_DEBUG
            world.Logger.LogDebug($"Module {GetType().GetTypeName()} systems pre-init", LogFilter.SystemsInit);
#endif

            PreInitSystems();

#if MODULES_DEBUG
            world.Logger.LogDebug($"Module {GetType().GetTypeName()} systems init", LogFilter.SystemsInit);
#endif

            InitSystems();
            RegisterInitSubscribers();

            _systemsArr = _systems.Values.ToArray();
        }

        protected virtual void PreInitSystems()
        {
            foreach (var p in _systems)
                p.Value.PreInit(world);
        }

        protected virtual void InitSystems()
        {
            foreach (var p in _systems)
                p.Value.Init(world);
        }

        private void RegisterInitSubscribers()
        {
            foreach (var p in _systems)
            {
                foreach (var subscriptionType in p.Value.SubscriptionTypes)
                {
                    RegisterSubscriber(subscriptionType, p.Value, true);
                }
            }
        }

        /// <summary>
        /// Turn on/off the module.
        /// If false, IRunSystem will to be updated
        /// </summary>
        /// <param name="isActive">Flag to turn on/off the module</param>
        internal void SetActive(bool isActive)
        {
#if MODULES_DEBUG
            var logMsgStart = isActive ? "activate" : "deactivate";
            world.Logger.LogDebug($"Start {logMsgStart} module {GetType().GetTypeName()}", LogFilter.ModulesFull);
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
                SetSubmodulesActive(false);
                SetActiveComposition(false);
                Deactivate();
                OnDeactivate();
                OnDeactivated?.Invoke();
            }

            IsActive = isActive;
        }

        private void Activate()
        {
#if MODULES_DEBUG
            world.Logger.LogDebug($"Activate systems in {GetType().GetTypeName()}", LogFilter.SystemsInit);
#endif
            foreach (var p in _systems)
            {
                foreach (var eventType in p.Value.EventTypes)
                {
                    foreach (var systemGenericType in p.Value.GetEventSystemsGenericTypes(eventType))
                        RegisterSystemsGroupForEvent(eventType, systemGenericType, p.Value);
                }

                foreach (var eventType in p.Value.SubscriptionTypes)
                    RegisterSubscriber(eventType, p.Value);
            }

            ActivateSystems();
#if MODULES_DEBUG
            world.Logger.LogDebug($"Call OnActivate in {GetType().GetTypeName()}", LogFilter.ModulesFull);
#endif
        }

        protected virtual void ActivateSystems()
        {
            foreach (var p in _systems)
                p.Value.Activate(world);
        }

        private void Deactivate()
        {
#if MODULES_DEBUG
            world.Logger.LogDebug($"Deactivate systems in {GetType().GetTypeName()}", LogFilter.SystemsDestroy);
#endif

            foreach (var (_, runners) in _eventRunners)
            {
                foreach (var (_, runner) in runners)
                {
                    runner.Clear();
                }
            }

            DeactivateSystems();
            foreach (var p in _systems)
            {
                foreach (var eventType in p.Value.EventTypes)
                {
                    foreach (var systemGenericType in p.Value.GetEventSystemsGenericTypes(eventType))
                        UnregisterSystemsGroupForEvent(eventType, systemGenericType, p.Value);
                }

                foreach (var eventType in p.Value.SubscriptionTypes)
                    UnregisterSubscriber(eventType, p.Value);
            }
#if MODULES_DEBUG
            world.Logger.LogDebug($"Call OnDeactivate in {GetType().GetTypeName()}", LogFilter.ModulesFull);
#endif
        }

        protected virtual void DeactivateSystems()
        {
            foreach (var p in _systems)
                p.Value.Deactivate(world);
        }

        /// <summary>
        /// Run composed modules, its own systems and submodules
        /// </summary>
        internal void Run()
        {
            if (!IsActive)
                return;

            foreach (var module in _composedModules)
            {
                module.Run();
            }

            RunSystems();

            foreach (var group in _submodulesGroups)
            {
                foreach (var submodule in group.modules)
                {
                    submodule.Run();
                }
            }
        }

        /// <summary>
        ///     Run systems of this module
        /// </summary>
        public virtual void RunSystems()
        {
            foreach (var p in _systemsArr)
            {
                foreach (var eventType in p.EventTypes)
                    RunEvents(eventType);
            }

            foreach (var p in _systemsArr)
            {
                p.Run(world);
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
        /// Calls after PreInitSystems and InitSystems called
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

        private void DestroySystemsInternal()
        {
#if MODULES_DEBUG
            world.Logger.LogDebug($"Destroy systems in {GetType().GetTypeName()}", LogFilter.SystemsDestroy);
#endif
            DestroySystems();
            foreach (var p in _systems)
            {
                foreach (var subscriptionType in p.Value.SubscriptionTypes)
                    UnregisterSubscriber(subscriptionType, p.Value, true);
            }

            IsInitialized = false;
        }

        protected virtual void DestroySystems()
        {
            foreach (var p in _systems)
                p.Value.Destroy(world);
        }

        /// <summary>
        /// For internal usage only
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Destroy()
        {
            if (!IsInitialized)
            {
                world.Logger.LogWarning($"Destroy module {GetType().GetTypeName()} that not initialized");
                return;
            }

#if MODULES_DEBUG
            world.Logger.LogDebug($"Start destroy module {GetType().GetTypeName()}", LogFilter.ModulesFull);
#endif

            // even if module was manually activate it still must be deactivated when parent module destroyed
            foreach (var group in _submodulesGroups)
            {
                foreach (var submodule in group.modules)
                {
                    if (submodule.IsActive)
                        submodule.SetActive(false);
                }
            }

            SetActive(false);

            // any submodule must be destroyed with parent cause it has dependencies from it that may being destroyed
            foreach (var group in _submodulesGroups)
            {
                foreach (var submodule in group.modules)
                {
                    if (submodule.IsInitialized)
                        submodule.Destroy();
                }
            }

            OnDestroy();
            #if MODULES_DEBUG
            UnregisterWatchers();
            #endif
            DestroySystemsInternal();

            foreach (var composedModule in _composedModules)
            {
                composedModule.Destroy();
            }

            _isSetup = false;
            IsInitialized = false;
            OnDestroyed?.Invoke();
        }

        private void ThrowOneDataException(Type t)
        {
            throw new ApplicationException(
                $"Type {t.GetGenericArguments()[0]} does not exist. You should use {nameof(DataWorld.OneData)}");
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
                dependency = Parent!.GetDependency(type);
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

        protected void InsertDependencies(object system)
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

                    object? dependency = GetDependency(t);

                    if (dependency == null)
                    {
                        foreach (var module in _globalModules)
                        {
                            dependency = module.GetDependency(t);
                            if (dependency != null)
                                break;
                        }
                    }

                    if (dependency == null)
                    {
                        throw new Exception(
                            $"Can't find injection {parameter.ParameterType} in method {setupMethod.Name}" +
                            $" for system {system.GetType().GetTypeName()}");
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

                object? dependency = GetDependency(t);

                if (dependency == null)
                {
                    foreach (var module in _globalModules)
                    {
                        dependency = module.GetDependency(t);
                        if (dependency != null)
                            break;
                    }
                }

                if (dependency != null)
                    field.SetValue(system, dependency);
                else
                    world.Logger.LogDebug(
                        $"Can't inject dependency for {field.Name} for system {system.GetType().GetTypeName()}." +
                        " Ignore this message if you create field by yourself",
                        LogFilter.ModulesFull
                    );
            }
        }

        private MethodInfo? GetSetupMethod(object system)
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
    }
}
