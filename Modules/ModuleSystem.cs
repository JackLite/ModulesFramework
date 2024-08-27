using ModulesFramework.Systems;

namespace ModulesFramework.Modules
{
    /// <summary>
    /// Internal system for controlling activation and deactivation of modules
    /// </summary>
    internal class ModuleSystem : IRunSystem, IRunPhysicSystem, IPostRunSystem
    {
        private readonly EcsModule[] _modules;

        internal ModuleSystem(EcsModule[] modules)
        {
            _modules = modules;
        }

        public void Run()
        {
            foreach (var module in _modules)
            {
                if (module.IsActive && !module.IsSubmodule)
                    module.Run();
            }
        }

        public void RunPhysic()
        {
            foreach (var module in _modules)
            {
                if (module.IsActive && !module.IsSubmodule)
                    module.RunPhysics();
            }
        }

        public void PostRun()
        {
            foreach (var module in _modules)
            {
                if (module.IsActive && !module.IsSubmodule)
                    module.PostRun();
            }
        }

        public void FrameEnd()
        {
            foreach (var module in _modules)
            {
                if (module.IsActive && !module.IsSubmodule)
                    module.FrameEnd();
            }
        }
    }
}