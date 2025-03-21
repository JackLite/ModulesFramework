using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ModulesFramework.Data;
using ModulesFramework.Exceptions;
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

        private readonly Dictionary<string, DataWorld> _worldsMap = new();
        private DataWorld?[] _worlds = new DataWorld?[64];
        private Queue<int> _freeWorldsIndices = new Queue<int>(64);
        private readonly MFCache _cache;
        public DataWorld MainWorld => _worlds[0]!;
        public IEnumerable<DataWorld> Worlds => _worldsMap.Values;
        private static MF Instance { get; set; }

        public static bool IsInitialized => Instance is { _isInitialized: true };
        public static DataWorld World => Instance.MainWorld;

        public MF(AssemblyFilter? filter = null)
        {
            var assemblyFilter = filter ?? new AssemblyFilter();
            _cache = new MFCache(assemblyFilter);
            Instance = this;
            CreateMainWorld();
            CreateEmbedded();
        }

        public static DataWorld GetWorld(string worldName)
        {
            if (!Instance._worldsMap.TryGetValue(worldName, out var world))
                throw new WorldNotFoundException(worldName);
            return world;
        }

        public static DataWorld GetWorld(int worldIndex)
        {
            if (Instance._worlds.Length <= worldIndex || Instance._worlds[worldIndex] == null)
                throw new WorldNotFoundException(worldIndex);
            return Instance._worlds[worldIndex];
        }

        public static DataWorld CreateWorld(string worldName)
        {
            var index = Instance.CreateWorldInternal(worldName);
            return Instance._worlds[index];
        }

        public static void DestroyWorld(DataWorld world)
        {
            Instance._worlds[world.WorldIndex] = null;
            Instance._worldsMap.Remove(world.WorldName);
            world.Destroy();
            Instance._freeWorldsIndices.Enqueue(world.WorldIndex);
        }

        public static IEnumerable<DataWorld> GetAllWorlds()
        {
            return Instance._worldsMap.Values;
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

        private int CreateWorldInternal(string name)
        {
            var index = _freeWorldsIndices.Count > 0 ? _freeWorldsIndices.Dequeue() : _worldsMap.Count;
            var world = new DataWorld(index, name, _cache.AllSystemTypes, _cache.AllModuleTypes);
            while (index >= _worlds.Length)
                Array.Resize(ref _worlds, _worlds.Length * 2);
            _worlds[index] = world;
            _worldsMap.Add(name, world);
            var moduleSystem = new ModuleSystem(world.GetAllModules().ToArray());
            _moduleSystems.Add(moduleSystem);
            return index;
        }

        public async Task Start()
        {
            try
            {
                await _embeddedGlobalModule.Init(true);
                foreach (var world in _worldsMap.Values)
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

            foreach (var world in _worldsMap.Values)
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
