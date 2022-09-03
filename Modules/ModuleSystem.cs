using System.Linq;
using Core;

namespace EcsCore
{
    /// <summary>
    /// Internal system for controlling activation and deactivation of modules
    /// </summary>
    internal class ModuleSystem : IRunSystem, IRunPhysicSystem, IPostRunSystem, IDestroySystem
    {
        private readonly DataWorld _world;
        private readonly EcsModule[] _modules;

        internal ModuleSystem(DataWorld world, ModulesRepository modulesRepository)
        {
            _world = world;
            _modules = modulesRepository.LocalModules.ToArray();
        }

        public void Run()
        {
            CheckSignals();

            foreach (var module in _modules)
            {
                if (module.IsInitialized())
                    module.Run();
            }
        }

        public void RunPhysic()
        {
            CheckSignals();

            foreach (var module in _modules)
            {
                if (module.IsInitialized())
                    module.RunPhysics();
            }
        }

        public void PostRun()
        {
            CheckSignals();

            foreach (var module in _modules)
            {
                if (module.IsInitialized())
                    module.PostRun();
            }
        }

        public void Destroy()
        {
            foreach (var module in _modules)
            {
                module.Destroy();
            }
        }

        private void CheckSignals()
        {
            CheckDestroySignals();
            CheckInitSignals();
            CheckChangeStateSignals();
        }

        private void CheckChangeStateSignals()
        {
            var changeStateEntities = _world.Select<ModuleChangeStateSignal>().GetEntities();
            foreach (var entity in changeStateEntities)
            {
                var signal = entity.GetComponent<ModuleChangeStateSignal>();
                var module = _modules.FirstOrDefault(m => m.GetType() == signal.moduleType);
                if (module == null)
                {
                    entity.Destroy();
                    continue;
                }

                if (!module.IsInitialized())
                    continue;

                module.SetActive(signal.state);
                entity.Destroy();
            }
        }

        private void CheckInitSignals()
        {
            var initEntities = _world.Select<ModuleInitSignal>().GetEntities();
            foreach (var entity in initEntities)
            {
                var initSignal = entity.GetComponent<ModuleInitSignal>();
                var module = _modules.FirstOrDefault(m => m.GetType() == initSignal.moduleType);
                if (module != null && !module.IsInitialized())
                {
                    EcsModule parent = null;
                    if (initSignal.dependenciesModule != null)
                        parent = _modules.FirstOrDefault(m => m.GetType() == initSignal.dependenciesModule);
                    module.Init(_world, parent).Forget();
                }

                entity.Destroy();
            }
        }

        private void CheckDestroySignals()
        {
            var destroyEntities = _world.Select<ModuleDestroySignal>().GetEntities();
            foreach (var entity in destroyEntities)
            {
                var type = entity.GetComponent<ModuleDestroySignal>().ModuleType;
                var module = _modules.FirstOrDefault(m => m.GetType() == type);
                if (module != null && module.IsInitialized())
                    module.Deactivate();
                entity.Destroy();
            }
        }
    }
}