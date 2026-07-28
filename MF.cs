using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ModulesFramework.Data;
using ModulesFramework.Exceptions;
using ModulesFramework.Utils;

namespace ModulesFramework
{
    public class MF
    {
        private bool _isInitialized;

        private readonly Dictionary<string, DataWorld> _worldsMap = new();
        private DataWorld?[] _worlds = new DataWorld?[64];
        private readonly Queue<int> _freeWorldsIndices = new Queue<int>(64);
        private readonly MFCache _cache;
        public DataWorld MainWorld => _worlds[0]!;
        public WorldEnumerable Worlds => new WorldEnumerable(_worlds);
        private static MF Instance { get; set; }

        public static bool IsInitialized => Instance is { _isInitialized: true };
        public static DataWorld World => Instance.MainWorld;

        public MF(AssemblyFilter? filter = null)
        {
            var assemblyFilter = filter ?? new AssemblyFilter();
            _cache = new MFCache(assemblyFilter);
            Instance = this;
            CreateMainWorld();
        }

        public static DataWorld GetWorld(string worldName)
        {
            if (!Instance._worldsMap.TryGetValue(worldName, out var world))
                throw new WorldNotFoundException(worldName);
            return world;
        }

        public static DataWorld? GetWorld(int worldIndex)
        {
            if (Instance._worlds.Length <= worldIndex || Instance._worlds[worldIndex] == null)
                throw new WorldNotFoundException(worldIndex);
            return Instance._worlds[worldIndex];
        }

        public static DataWorld CreateWorld(string worldName)
        {
            var index = Instance.CreateWorldInternal(worldName);
            return Instance._worlds[index]!;
        }

        public static bool IsWorldExists(string worldName)
        {
            return Instance._worldsMap.ContainsKey(worldName);
        }

        public static void DestroyWorld(DataWorld world)
        {
            Instance._worlds[world.WorldIndex] = null;
            Instance._worldsMap.Remove(world.WorldName);
            world.Destroy();
            Instance._freeWorldsIndices.Enqueue(world.WorldIndex);
        }

        public static WorldEnumerable GetAllWorlds()
        {
            return Instance.Worlds;
        }
        
        private void CreateMainWorld()
        {
            CreateWorld("Default");
        }

        private int CreateWorldInternal(string name)
        {
            var index = _freeWorldsIndices.Count > 0 ? _freeWorldsIndices.Dequeue() : _worldsMap.Count;
            var world = new DataWorld(index, name, _cache);
            while (index >= _worlds.Length)
                Array.Resize(ref _worlds, _worlds.Length * 2);
            _worlds[index] = world;
            _worldsMap.Add(name, world);
            return index;
        }

        public async Task Start()
        {
            var tasks = new List<Task>();
            foreach (var world in _worldsMap.Values.ToList())
            {
                tasks.Add(world.Start());
            }

            await Task.WhenAll(tasks);

            _isInitialized = true;
        }

        public void Run()
        {
            if (!_isInitialized)
                return;

            foreach (var world in _worldsMap.Values)
            {
                world.Run();
            }
        }

        public void Destroy()
        {
            if (!_isInitialized)
                return;

            foreach (var world in _worldsMap.Values)
            {
                world.Destroy();
            }
        }

        public readonly struct WorldEnumerable
        {
            private readonly DataWorld?[] _worlds;

            public WorldEnumerable(DataWorld?[] worlds)
            {
                _worlds = worlds;
            }
            
            public NullableArrayEnumerator<DataWorld> GetEnumerator()
            {
                return new NullableArrayEnumerator<DataWorld>(_worlds);
            }
        }
    }
}
