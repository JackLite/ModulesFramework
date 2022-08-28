using System;
using System.Collections.Generic;
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
        private readonly IReadOnlyCollection<EcsModule> _modules;

        internal ModuleSystem(DataWorld world, ModulesRepository modulesRepository)
        {
            _world = world;
            _modules = modulesRepository.LocalModules;
        }

        public void Run()
        {
            CheckActivationAndDeactivation();

            foreach (var module in _modules)
            {
                if (module.IsActiveAndInitialized())
                    module.Run();
            }
        }

        private void CheckActivationAndDeactivation()
        {
            var deactivationEntities = _world.Select<ModuleDeactivationSignal>().GetEntities();
            foreach (var entity in deactivationEntities)
            {
                var type = entity.GetComponent<ModuleDeactivationSignal>().ModuleType;
                var module = _modules.FirstOrDefault(m => m.GetType() == type);
                if (module != null && module.IsActiveAndInitialized())
                    module.Deactivate();
                entity.Destroy();
            }

            var activationEntities = _world.Select<ModuleActivationSignal>().GetEntities();
            foreach (var entity in activationEntities)
            {
                var activationSignal = entity.GetComponent<ModuleActivationSignal>();
                var module = _modules.FirstOrDefault(m => m.GetType() == activationSignal.moduleType);
                if (module != null && !module.IsActiveAndInitialized())
                {
                    EcsModule parent = null;
                    if (activationSignal.dependenciesModule != null)
                        parent = _modules.FirstOrDefault(m => m.GetType() == activationSignal.dependenciesModule);
                    module.Activate(_world, parent);
                }

                entity.Destroy();
            }
        }

        public void Destroy()
        {
            foreach (var module in _modules)
            {
                module.Destroy();
            }
        }
        public void RunPhysic()
        {
            CheckActivationAndDeactivation();

            foreach (var module in _modules)
            {
                if (module.IsActiveAndInitialized())
                    module.RunPhysics();
            }
        }
        public void PostRun()
        {
            CheckActivationAndDeactivation();

            foreach (var module in _modules)
            {
                if (module.IsActiveAndInitialized())
                    module.PostRun();
            }
        }
    }
}