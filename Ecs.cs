using System;
using System.Linq;
using ModulesFramework.Data;
using ModulesFramework.Modules;

namespace ModulesFramework
{
    public class Ecs
    {
        private EcsModule[] _globalModules = Array.Empty<EcsModule>();
        private bool _isInitialized;
        private readonly ModuleSystem _moduleSystem;
        private readonly EmbeddedGlobalModule _embeddedGlobalModule;

        public DataWorld World { get; }

        public Ecs()
        {
            World = new DataWorld();
            _moduleSystem = new ModuleSystem(World.GetAllModules().ToArray());
            _embeddedGlobalModule = new EmbeddedGlobalModule();
            _embeddedGlobalModule.InjectWorld(World);
        }

        public async void Start()
        {
            await _embeddedGlobalModule.Init(true);
            _globalModules = World.GetAllModules().Where(m => m.IsGlobal).ToArray();
            foreach (var module in _globalModules)
            {
                await module.Init(true);
            }

            _isInitialized = true;
        }

        public void Run()
        {
            if (!_isInitialized)
                return;
            if (ExceptionsPool.TryPop(out var e))
                throw e;
            
            _embeddedGlobalModule.Run();
            _moduleSystem.Run();
        }

        public void PostRun()
        {
            if (!_isInitialized)
                return;
            
            _embeddedGlobalModule.PostRun();
            _moduleSystem.PostRun();
        }

        public void RunPhysic()
        {
            if (!_isInitialized)
                return;
            
            _embeddedGlobalModule.RunPhysics();
            _moduleSystem.RunPhysic();
        }

        public void Destroy()
        {
            if (!_isInitialized)
                return;
            
            foreach (var module in World.GetAllModules().Where(m => !m.IsSubmodule))
            {
                if (module.IsActive)
                    module.SetActive(false);

                if (module.IsInitialized)
                    module.Destroy();
            }
            
            _embeddedGlobalModule.Destroy();
        }
    }
}