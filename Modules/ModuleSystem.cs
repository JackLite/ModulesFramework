using System.Linq;
using ModulesFramework.Data;
using ModulesFramework.Systems;

namespace ModulesFramework.Modules
{
    /// <summary>
    /// Internal system for controlling activation and deactivation of modules
    /// </summary>
    internal class ModuleSystem : IRunSystem, IRunPhysicSystem, IPostRunSystem, IDestroySystem
    {
        private readonly EcsModule[] _modules;

        internal ModuleSystem(DataWorld world)
        {
            _modules = world.GetAllModules().ToArray();
        }

        public void Run()
        {
            foreach (var module in _modules)
            {
                if (module.IsActive)
                    module.Run();
            }
        }

        public void RunPhysic()
        {
            foreach (var module in _modules)
            {
                if (module.IsActive)
                    module.RunPhysics();
            }
        }

        public void PostRun()
        {
            foreach (var module in _modules)
            {
                if (module.IsActive)
                    module.PostRun();
            }
        }

        public void Destroy()
        {
            foreach (var module in _modules)
            {
                if (module.IsActive)
                    module.SetActive(false);

                if (module.IsInitialized())
                    module.Destroy();
            }
        }
    }
}