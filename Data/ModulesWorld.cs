#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ModulesFramework.Attributes;
using ModulesFramework.Exceptions;
using ModulesFramework.Modules;
using ModulesFramework.Systems;
using ModulesFramework.Utils;
using ModulesFramework.Utils.Types;

namespace ModulesFramework.Data
{
    public partial class DataWorld
    {
        private readonly Map<EcsModule> _modules;
        private Dictionary<Type, List<Type>>? _allSystemTypes;
        private EmbeddedGlobalModule _embeddedGlobalModule;

        /// <summary>
        /// Init module: call Setup() and GetDependencies()
        /// You must activate module for IRunSystem, IRunPhysicSystem and IPostRunSystem
        /// </summary>
        /// <typeparam name="T">Type of module that you want to activate</typeparam>
        /// <seealso cref="ActivateModule{T}"/>
        /// <seealso cref="InitModule"/>
        /// <seealso cref="InitModuleAsync{T}"/>
        public void InitModule<T>(bool activateImmediately = false) where T : EcsModule
        {
            InitModule(typeof(T), activateImmediately);
        }

        /// <summary>
        /// Init module: call Setup() and GetDependencies()
        /// You must activate module for IRunSystem, IRunPhysicSystem and IPostRunSystem
        /// </summary>
        /// <seealso cref="ActivateModule{T}"/>
        /// <seealso cref="InitModule{T}"/>
        /// <seealso cref="InitModuleAsync"/>
        public void InitModule(Type moduleType, bool activateImmediately = false)
        {
            InitModuleAsync(moduleType, activateImmediately).Forget();
        }

        /// <summary>
        /// Init module asynchronously. Call Setup() and GetDependencies()
        /// You must activate module for IRunSystem, IRunPhysicSystem and IPostRunSystem
        /// </summary>
        /// <seealso cref="ActivateModule{T}"/>
        /// <seealso cref="InitModule{T}"/>
        /// <seealso cref="InitModuleAsync"/>
        public async Task InitModuleAsync<T>(bool activateImmediately = false)
        {
            await InitModuleAsync(typeof(T), activateImmediately);
        }

        /// <summary>
        /// Init module asynchronously. Call Setup() and GetDependencies()
        /// You must activate module for IRunSystem, IRunPhysicSystem and IPostRunSystem
        /// </summary>
        /// <seealso cref="ActivateModule{T}"/>
        /// <seealso cref="InitModule{T}"/>
        /// <seealso cref="InitModuleAsync{T}"/>
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
            Logger.LogDebug($"Activate module {moduleType.GetTypeName()}", LogFilter.ModulesFull);
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
            Logger.LogDebug($"Deactivate module {moduleType.GetTypeName()}", LogFilter.ModulesFull);
#endif
            module.SetActive(false);
        }

        /// <summary>
        ///     Return true if module T is initialized and active
        /// </summary>
        public bool IsModuleActive<TModule>() where TModule : EcsModule
        {
            var localModule = GetModule<TModule>();
            return localModule is { IsActive: true };
        }

        /// <summary>
        ///     Return module T
        /// </summary>
        /// <typeparam name="T">Type of module. It must inherit from EcsModule</typeparam>
        public T GetModule<T>() where T : EcsModule
        {
            if (_modules.TryGet<T>(out var module))
                return (T)module;
            throw new ModuleNotFoundException(typeof(T));
        }

        private void CtorModules(Dictionary<Type, EcsModule> modules)
        {
            foreach (var (_, module) in modules)
            {
                var add = _modules.GetType().GetMethod(nameof(Map<object>.Add))!.MakeGenericMethod(module.GetType());
                add.Invoke(_modules, new object[] { module });
                var submoduleAttr = module.GetType().GetCustomAttribute<SubmoduleAttribute>();
                if (submoduleAttr != null)
                {
                    var parent = modules[submoduleAttr.parent];
                    module.MarkSubmodule(
                        parent,
                        submoduleAttr.initWithParent,
                        submoduleAttr.activeWithParent
                    );
                    parent.AddSubmodule(module);
                }
                
            }
        }

        private Dictionary<Type, EcsModule> CreateAllEcsModules(List<Type> moduleTypes)
        {
            var result = new Dictionary<Type, EcsModule>();
            foreach (var moduleType in moduleTypes)
            {
                var worldAttribute = moduleType.GetCustomAttribute<WorldBelongingAttribute>();
                if (worldAttribute == null || worldAttribute.Worlds.Contains(WorldName))
                {
                    var module = (EcsModule)Activator.CreateInstance(moduleType)!;
                    module.InjectWorld(this);
                    result.Add(moduleType, module);
                }
            }

            foreach (var module in result.Values)
            {
                foreach (var composedModule in module.ComposedOf)
                {
                    result[composedModule].IsComposed = true;
                    module.AddComposedModule(result[composedModule]);
                }
            }

            return result;
        }

        /// <summary>
        ///     Return all created modules
        /// </summary>
        public IEnumerable<EcsModule> GetAllModules()
        {
            return _modules.Values;
        }

        /// <summary>
        ///     Return module by its type. There is no case when you can't get module if
        ///     it's inherited from <see cref="EcsModule"/> class and MF is started
        /// </summary>
        /// <param name="moduleType">Type of module</param>
        /// <returns>EcsModule</returns>
        /// <exception cref="ModuleNotFoundException"></exception>
        public EcsModule GetModule(Type moduleType)
        {
            var module = _modules.Find(m => m != null && m.GetType() == moduleType);
            if (module != null)
                return module;
            throw new ModuleNotFoundException(moduleType);
        }

        internal IEnumerable<ISystem> GetSystems(Type moduleType)
        {
            if (_allSystemTypes == null || !_allSystemTypes.TryGetValue(moduleType, out var systems))
                yield break;

            foreach (var system in systems)
            {
                yield return (ISystem)Activator.CreateInstance(system)!;
            }
        }
        
        private void CreateEmbedded()
        {
            _embeddedGlobalModule = new EmbeddedGlobalModule();
            _embeddedGlobalModule.InjectWorld(this);
        }
    }
}
