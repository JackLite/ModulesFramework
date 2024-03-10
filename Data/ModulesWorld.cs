using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ModulesFramework.Attributes;
using ModulesFramework.Exceptions;
using ModulesFramework.Modules;

namespace ModulesFramework.Data
{
    public partial class DataWorld
    {
        private readonly Dictionary<Type, EcsModule> _modules;
        private readonly Dictionary<Type, List<EcsModule>> _submodules;

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
            try
            {
                var module = GetModule(moduleType);
                #if MODULES_DEBUG
                if (module == null) throw new ModuleNotFoundException(moduleType);
                if (module.IsInitialized)
                    throw new ModuleAlreadyInitializedException(moduleType);
                #endif
                await module.Init(activateImmediately);
            }
            catch (Exception e)
            {
                Logger.RethrowException(e);
            }
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

        public EcsModule GetModule<T>() where T : EcsModule
        {
            return GetModule(typeof(T));
        }

        private void CtorModules(int worldIndex)
        {
            var modules = CreateAllEcsModules(worldIndex).ToDictionary(m => m.GetType(), m => m);
            foreach (var (_, module) in modules)
            {
                _modules.Add(module.GetType(), module);
                var submoduleAttr = module.GetType().GetCustomAttribute<SubmoduleAttribute>();
                if (submoduleAttr != null)
                {
                    module.MarkSubmodule(
                        modules[submoduleAttr.parent],
                        submoduleAttr.initWithParent,
                        submoduleAttr.activeWithParent
                    );
                    if (!_submodules.ContainsKey(submoduleAttr.parent))
                        _submodules[submoduleAttr.parent] = new List<EcsModule>();
                    _submodules[submoduleAttr.parent].Add(module);
                }
            }
        }

        private IEnumerable<EcsModule> CreateAllEcsModules(int worldIndex)
        {
            var modules = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes()
                    .Where(t => t != typeof(EmbeddedGlobalModule))
                    .Where(t => t.IsSubclassOf(typeof(EcsModule)) && !t.IsAbstract)
                    .Select(t => (EcsModule)Activator.CreateInstance(t)));
            foreach (var module in modules)
            {
                if (!module.WorldIndex.Contains(worldIndex))
                    continue;
                module.InjectWorld(this);
                yield return module;
            }
        }

        public IEnumerable<EcsModule> GetAllModules()
        {
            return _modules.Values;
        }

        public EcsModule GetModule(Type moduleType)
        {
            if (_modules.TryGetValue(moduleType, out var module))
                return module;
            throw new ModuleNotFoundException(moduleType);
        }

        internal IEnumerable<EcsModule> GetSubmodules(Type parent)
        {
            if (_submodules.ContainsKey(parent))
                return _submodules[parent];
            return Array.Empty<EcsModule>();
        }
    }
}