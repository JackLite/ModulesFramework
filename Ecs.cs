using System.Collections.Generic;
using EcsCore;

namespace Core
{
    public class Ecs
    {
        private ModulesRepository _modulesRepository;
        private  IReadOnlyCollection<EcsModule> _globalModules;
        private DataWorld _world;
        private bool _isInitialized;
        private ModuleSystem _moduleSystem;

        public DataWorld World => _world;

        public Ecs()
        {
            _modulesRepository = new ModulesRepository();
            _world = new DataWorld();
            _moduleSystem = new ModuleSystem(_world, _modulesRepository);
        }
        
        public async void Start()
        {
            // находим все глобальные модули
            _globalModules = _modulesRepository.GlobalModules;
            // активируем их
            foreach (var module in _globalModules)
            {
                await module.Activate(_world);
            }
            _isInitialized = true;
        }

        public void Run()
        {
            if (!_isInitialized)
                return;
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
                module.Destroy();
            }
        }
    }
}