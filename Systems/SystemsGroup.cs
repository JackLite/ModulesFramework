using System;
using System.Collections.Generic;

namespace ModulesFramework.Systems
{
    public class SystemsGroup : IPreInitSystem, IInitSystem, IRunSystem, IRunPhysicSystem, IPostRunSystem, IDestroySystem
    {
        private readonly Dictionary<Type, List<ISystem>> _systems = new Dictionary<Type, List<ISystem>>();
        private static readonly Type[] _systemTypes = new Type[]
        {
            typeof(IPreInitSystem),
            typeof(IInitSystem),
            typeof(IActivateSystem),
            typeof(IRunSystem),
            typeof(IRunPhysicSystem),
            typeof(IPostRunSystem),
            typeof(IDeactivateSystem),
            typeof(IDestroySystem)
        };

        public SystemsGroup()
        {
            foreach (var type in _systemTypes)
            {
                _systems[type] = new List<ISystem>(64);
            }
        }

        public void PreInit()
        {
            foreach (var s in _systems[typeof(IPreInitSystem)])
                ((IPreInitSystem) s).PreInit();
        }

        public void Init()
        {
            foreach (var s in _systems[typeof(IInitSystem)])
                ((IInitSystem) s).Init();
        }

        public void Activate()
        {
            foreach (var s in _systems[typeof(IActivateSystem)])
                ((IActivateSystem) s).Activate();
        }
        
        public void Run()
        {
            foreach (var s in _systems[typeof(IRunSystem)])
                ((IRunSystem) s).Run();
        }
        
        public void RunPhysic()
        {
            foreach (var s in _systems[typeof(IRunPhysicSystem)])
                ((IRunPhysicSystem) s).RunPhysic();
        }
        
        public void PostRun()
        {
            foreach (var s in _systems[typeof(IPostRunSystem)])
                ((IPostRunSystem) s).PostRun();
        }
        
        public void Deactivate()
        {
            foreach (var s in _systems[typeof(IDeactivateSystem)])
                ((IDeactivateSystem) s).Deactivate();
        }
        
        public void Destroy()
        {
            foreach (var s in _systems[typeof(IDestroySystem)])
                ((IDestroySystem) s).Destroy();
        }

        public void Add(ISystem s)
        {
            foreach (var type in _systemTypes)
            {
                if (type.IsInstanceOfType(s))
                {
                    _systems[type].Add(s);
                }
            }
        }
    }
}