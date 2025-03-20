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
        private readonly List<ModuleSystem> _moduleSystems = new();
        private EmbeddedGlobalModule _embeddedGlobalModule;

        private readonly Dictionary<string, DataWorld> _worlds = new();
        private readonly MFCache _cache;
        public DataWorld MainWorld => _worlds["Default"];
        public IEnumerable<DataWorld> Worlds => _worlds.Values;
        private static MF Instance { get; set; }

        public static bool IsInitialized => Instance is { _isInitialized: true };
        public static DataWorld World => Instance.MainWorld;

        public MF(AssemblyFilter? filter = null)
        {
            var assemblyFilter = filter ?? new AssemblyFilter();
            _cache = new MFCache(assemblyFilter);
            CreateMainWorld();
            CreateEmbedded();
            Instance = this;
        }

        public DataWorld GetWorld(string worldName)
        {
            return _worlds[worldName];
        }

        public static DataWorld FromWorld(string worldName)
        {
            return Instance.GetWorld(worldName);
        }

        public IEnumerable<DataWorld> GetAllWorlds()
        {
            return _worlds.Values;
        }

        private void CreateEmbedded()
        {
            _embeddedGlobalModule = new EmbeddedGlobalModule();
            _embeddedGlobalModule.InjectWorld(MainWorld);
        }

        private void CreateMainWorld()
        {
            CreateWorld("Default");
        }

        public int CreateWorld(string name)
        {
            var index = _worlds.Count;
            var world = new DataWorld(name, _cache.AllSystemTypes, _cache.AllModuleTypes);
            _worlds.Add(name, world);
            var moduleSystem = new ModuleSystem(world.GetAllModules().ToArray());
            _moduleSystems.Add(moduleSystem);
            return index;
        }

        public async Task Start()
        {
            try
            {
                await _embeddedGlobalModule.Init(true);
                foreach (var world in _worlds.Values)
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

            _embeddedGlobalModule.FrameEnd();
            foreach (var system in _moduleSystems)
            {
                system.FrameEnd();
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

            foreach (var world in _worlds.Values)
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
