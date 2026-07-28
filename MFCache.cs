using System;
using System.Collections.Generic;
using System.Reflection;
using ModulesFramework.Attributes;
using ModulesFramework.Modules;
using ModulesFramework.Systems;
using ModulesFramework.Utils;
using ModulesFramework.Utils.Types;
using ModulesFramework.Watchers;

namespace ModulesFramework
{
    /// <summary>
    ///     Caches all found systems and modules
    /// </summary>
    internal class MFCache
    {
        private readonly Dictionary<Type, List<Type>> _allSystems = new Dictionary<Type, List<Type>>();
        private readonly HashSet<Type> _allModules = new HashSet<Type>();
        
        #if MODULES_DEBUG
        private readonly WatchersTypeContainer _watchersTypeContainer = new WatchersTypeContainer();
        #endif

        public Dictionary<Type, List<Type>> AllSystemTypes => _allSystems;
        public HashSet<Type> AllModuleTypes => _allModules;
        
        #if MODULES_DEBUG
        public WatchersTypeContainer WatchersType => _watchersTypeContainer;
        #endif

        public MFCache(AssemblyFilter assemblyFilter)
        {
            var allTypes = TypeUtilities.GetTypes(assemblyFilter.Filter);
            foreach (var type in allTypes)
            {
                CheckSystem(type);
                CheckModule(type);
                
                #if MODULES_DEBUG
                _watchersTypeContainer.ProcessType(type);
                #endif
            }
        }

        private void CheckSystem(Type type)
        {
            if (!typeof(ISystem).IsAssignableFrom(type))
                return;

            var attr = type.GetCustomAttribute<EcsSystemAttribute>();
            var global = type.GetCustomAttribute<GlobalSystemAttribute>();
            if (attr == null && global == null)
                return;

            var module = attr != null ? attr.module : typeof(EmbeddedGlobalModule);
            if (!_allSystems.ContainsKey(module))
                _allSystems[module] = new List<Type>();

            _allSystems[module].Add(type);
        }

        private void CheckModule(Type type)
        {
            if (!type.IsSubclassOf(typeof(EcsModule)) || type.IsAbstract)
                return;

            if (type == typeof(EmbeddedGlobalModule))
                return;

            _allModules.Add(type);
        }
    }
}
