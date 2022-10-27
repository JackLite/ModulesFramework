using System.Linq;
using EcsCore;
using ModulesFramework.Modules;

namespace Core
{
    public class Ecs
    {
        private ModulesRepository _modulesRepository;
        private EcsModule[] _globalModules;
        private DataWorld _world;
        private bool _isInitialized;
        private ModuleSystem _moduleSystem;
        private EcsOneFrameSystem _oneFrameSystem;

        public DataWorld World => _world;

        public Ecs()
        {
            _modulesRepository = new ModulesRepository();
            _world = new DataWorld();
            _moduleSystem = new ModuleSystem(_world, _modulesRepository);
            _oneFrameSystem = new EcsOneFrameSystem(_world);
        }

        public async void Start()
        {
            // находим все глобальные модули
            _globalModules = _modulesRepository.GlobalModules.ToArray();
            // активируем их
            foreach (var module in _globalModules)
            {
                await module.Init(_world);
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

            foreach (var module in _globalModules)
            {
                module.Run();
            }
        }

        public void PostRun()
        {
            if (!_isInitialized)
                return;
            _moduleSystem.PostRun();

            foreach (var module in _globalModules)
            {
                module.PostRun();
            }
            
            _oneFrameSystem.PostRun();
        }

        public void RunPhysic()
        {
            if (!_isInitialized)
                return;
            _moduleSystem.RunPhysic();

            foreach (var module in _globalModules)
            {
                module.RunPhysics();
            }
        }

        public void Destroy()
        {
            if (!_isInitialized)
                return;
            _moduleSystem.Destroy();

            foreach (var module in _globalModules)
            {
                module.SetActive(false);
                module.Destroy();
            }
        }
    }
}