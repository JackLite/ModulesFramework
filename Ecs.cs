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
        private ModuleSystem[] _moduleSystems;
        private EmbeddedGlobalModule _embeddedGlobalModule;

        private DataWorld[] _worlds;
        public DataWorld MainWorld => _worlds[0];

        public Ecs()
        {
            CreateWorlds(1);
            CreateEmbedded();
        }

        public Ecs(int worldsCount)
        {
            CreateWorlds(worldsCount);
            CreateEmbedded();
        }
        
        public DataWorld GetWorld(int index)
        {
            return _worlds[index];
        }

        private void CreateEmbedded()
        {
            _embeddedGlobalModule = new EmbeddedGlobalModule();
            _embeddedGlobalModule.InjectWorld(MainWorld);
        }

        private void CreateWorlds(int count)
        {
            _worlds = new DataWorld[count];
            _moduleSystems = new ModuleSystem[count];
            for (var i = 0; i < count; i++)
            {
                _worlds[i] = new DataWorld(i);
                _moduleSystems[i] = new ModuleSystem(_worlds[i].GetAllModules().ToArray());
            }
        }

        public async void Start()
        {
            await _embeddedGlobalModule.Init(true);
            foreach (var world in _worlds)
            {
                _globalModules = world.GetAllModules().Where(m => m.IsGlobal).ToArray();
                foreach (var module in _globalModules)
                {
                    await module.Init(true);
                }
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
            foreach (var system in _moduleSystems)
            {
                system.Run();
            }
        }

        public void PostRun()
        {
            if (!_isInitialized)
                return;

            _embeddedGlobalModule.PostRun();
            foreach (var system in _moduleSystems)
            {
                system.PostRun();
            }
        }

        public void RunPhysic()
        {
            if (!_isInitialized)
                return;

            _embeddedGlobalModule.RunPhysics();
            foreach (var system in _moduleSystems)
            {
                system.RunPhysic();
            }
        }

        public void Destroy()
        {
            if (!_isInitialized)
                return;

            foreach (var world in _worlds)
            {
                foreach (var module in world.GetAllModules().Where(m => !m.IsSubmodule))
                {
                    if (module.IsActive)
                        module.SetActive(false);

                    if (module.IsInitialized)
                        module.Destroy();
                }
            }

            _embeddedGlobalModule.Destroy();
        }
    }
}