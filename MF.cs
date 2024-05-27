using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ModulesFramework.Data;
using ModulesFramework.Modules;
using ModulesFramework.Utils;

namespace ModulesFramework
{
    public class MF
    {
        private EcsModule[] _globalModules = Array.Empty<EcsModule>();
        private bool _isInitialized;
        private ModuleSystem[] _moduleSystems;
        private EmbeddedGlobalModule _embeddedGlobalModule;

        private DataWorld[] _worlds;
        private AssemblyFilter _assemblyFilter;
        public DataWorld MainWorld => _worlds[0];
        public IEnumerable<DataWorld> Worlds => _worlds;
        private static MF Instance { get; set; }

        public static bool IsInitialized => Instance is { _isInitialized: true };
        public static DataWorld World => Instance.MainWorld;

        public MF(int worldsCount = 1, AssemblyFilter? filter = null)
        {
            _assemblyFilter = filter ?? new AssemblyFilter();
            CreateWorlds(worldsCount);
            CreateEmbedded();
            Instance = this;
        }

        public DataWorld GetWorld(int index)
        {
            return _worlds[index];
        }

        public IEnumerable<DataWorld> GetAllWorlds()
        {
            return _worlds;
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
                _worlds[i] = new DataWorld(i, _assemblyFilter);
                _moduleSystems[i] = new ModuleSystem(_worlds[i].GetAllModules().ToArray());
            }
        }

        public async Task Start()
        {
            try
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
            }
            catch (Exception e)
            {
                MainWorld.Logger.RethrowException(e);
                throw;
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