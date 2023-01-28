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
        private readonly EcsOneFrameSystem _oneFrameSystem;

        public DataWorld World { get; }

        public Ecs()
        {
            World = new DataWorld();
            _moduleSystem = new ModuleSystem(World.GetAllModules().ToArray());
            _oneFrameSystem = new EcsOneFrameSystem(World);
        }

        public async void Start()
        {
            _globalModules = World.GetAllModules().Where(m => m.IsGlobal).ToArray();
            foreach (var module in _globalModules)
            {
                await module.Init();
                module.SetActive(true);
            }

            _isInitialized = true;
        }

        public void Run()
        {
            if (!_isInitialized)
                return;
            if (ExceptionsPool.TryPop(out var e))
                throw e;
            _moduleSystem.Run();
        }

        public void PostRun()
        {
            if (!_isInitialized)
                return;
            _moduleSystem.PostRun();

            _oneFrameSystem.PostRun();
        }

        public void RunPhysic()
        {
            if (!_isInitialized)
                return;
            _moduleSystem.RunPhysic();
        }

        public void Destroy()
        {
            if (!_isInitialized)
                return;
            _moduleSystem.Destroy();
        }
    }
}