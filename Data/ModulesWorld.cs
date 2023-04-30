using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ModulesFramework.Exceptions;
using ModulesFramework.Modules;

namespace ModulesFramework.Data
{
    public partial class DataWorld
    {
        /// <summary>
        /// Init module: call Setup() and GetDependencies()
        /// You must activate module for IRunSystem, IRunPhysicSystem and IPostRunSystem
        /// </summary>
        /// <typeparam name="T">Type of module that you want to activate</typeparam>
        /// <seealso cref="ActivateModule{T}"/>
        /// <seealso cref="InitModule{T, T}"/>
        public void InitModule<T>(bool activateImmediately = false) where T : EcsModule
        {
            InitModule(typeof(T), activateImmediately);
        }

        /// <summary>
        /// Init module: call Setup() and GetDependencies()
        /// You must activate module for IRunSystem, IRunPhysicSystem and IPostRunSystem
        /// </summary>
        /// <seealso cref="ActivateModule{T}"/>
        /// <seealso cref="InitModule{T, T}"/>
        public void InitModule(Type moduleType, bool activateImmediately = false)
        {
            InitModuleAsync(moduleType, activateImmediately).Forget();
        }

        public async Task InitModuleAsync<T>(bool activateImmediately = false)
        {
            await InitModuleAsync(typeof(T), activateImmediately);
        }

        public async Task InitModuleAsync(Type moduleType, bool activateImmediately = false)
        {
            var module = GetModule(moduleType);
            #if MODULES_DEBUG
            if (module == null) throw new ModuleNotFoundException(moduleType);
            if (module.IsInitialized)
                throw new ModuleAlreadyInitializedException(moduleType);
            #endif
            await module.Init(activateImmediately);
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
            InitModule(typeof(TModule), typeof(TParent), activateImmediately);
        }

        /// <summary>
        /// Initialize module: call Setup() and GetDependencies()
        /// You must activate module for IRunSystem, IRunPhysicSystem and IPostRunSystem
        /// ATTENTION! Only local modules can be parent. Dependency from global modules
        /// available in all systems by default
        /// </summary>
        /// <seealso cref="InitModule{T}"/>
        /// <seealso cref="ActivateModule{T}"/>
        public void InitModule(Type moduleType, Type parentType, bool activateImmediately = false)
        {
            var module = GetModule(moduleType);
            var parent = GetModule(parentType);
            #if MODULES_DEBUG
            if (module == null) throw new ModuleNotFoundException(moduleType);
            if (parent == null) throw new ModuleNotFoundException(parentType);
            if (module.IsInitialized)
                throw new ModuleAlreadyInitializedException(moduleType);
            #endif
            module.Init(activateImmediately, parent).Forget();
        }

        /// <summary>
        /// Destroy module: calls Deactivate() in module and Destroy() in IDestroy systems
        /// </summary>
        /// <typeparam name="T">Type of module that you want to destroy</typeparam>
        public void DestroyModule<T>() where T : EcsModule
        {
            DestroyModule(typeof(T));
        }

        /// <summary>
        /// Destroy module: calls Deactivate() in module and Destroy() in IDestroy systems
        /// </summary>
        /// <typeparam name="T">Type of module that you want to destroy</typeparam>
        public void DestroyModule(Type moduleType)
        {
            var module = GetModule(moduleType);
            #if MODULES_DEBUG
            if (module == null) throw new ModuleNotFoundException(moduleType);
            Logger.LogDebug($"Destroy module {moduleType.Name}", LogFilter.ModulesFull);
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
            ActivateModule(typeof(T));
        }


        /// <summary>
        /// Activate module: IRunSystem, IRunPhysicSystem and IPostRunSystem will start update
        /// </summary>
        /// <seealso cref="InitModule{T}"/>
        /// <seealso cref="DeactivateModule{T}"/>
        public void ActivateModule(Type moduleType)
        {
            var module = GetModule(moduleType);
            #if MODULES_DEBUG
            if (module == null) throw new ModuleNotFoundException(moduleType);
            Logger.LogDebug($"Activate module {moduleType.Name}", LogFilter.ModulesFull);
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
            DeactivateModule(typeof(T));
        }

        /// <summary>
        /// Deactivate module: IRunSystem, IRunPhysicSystem and IPostRunSystem will stop update
        /// </summary>
        /// <seealso cref="DestroyModule{T}"/>
        /// <seealso cref="ActivateModule{T}"/>
        public void DeactivateModule(Type moduleType)
        {
            var module = GetModule(moduleType);
            #if MODULES_DEBUG
            if (module == null) throw new ModuleNotFoundException(moduleType);
            Logger.LogDebug($"Deactivate module {moduleType.Name}", LogFilter.ModulesFull);
            #endif
            module.SetActive(false);
        }

        public bool IsModuleActive<TModule>() where TModule : EcsModule
        {
            var localModule = GetModule<TModule>();
            return localModule is { IsActive: true };
        }

        public EcsModule? GetModule<T>() where T : EcsModule
        {
            return GetModule(typeof(T));
        }

        private IEnumerable<EcsModule> CreateAllEcsModules()
        {
            var modules = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes()
                    .Where(t => t != typeof(EmbeddedGlobalModule))
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

        public EcsModule? GetModule(Type moduleType)
        {
            if (_modules.TryGetValue(moduleType, out var module))
                return module;
            return null;
        }
    }
}