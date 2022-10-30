using System;
using System.Collections.Generic;
using System.Linq;

namespace ModulesFramework.Modules
{
    /// <summary>
    /// Encapsulate creation of modules and let take them by type
    /// Only for internal usage
    /// </summary>
    public class ModulesRepository
    {
        private readonly Dictionary<Type, EcsModule> _globalModules;
        private readonly Dictionary<Type, EcsModule> _localModules;

        internal IReadOnlyCollection<EcsModule> GlobalModules => _globalModules.Values;
        internal IReadOnlyCollection<EcsModule> LocalModules => _localModules.Values;

        internal ModulesRepository()
        {
            var allModules = CreateAllEcsModules().ToArray();
            for (var i = 0; i < allModules.Length; ++i)
                allModules[i].InjectRepository(this);

            _globalModules = allModules.Where(m => m.IsGlobal).ToDictionary(m => m.GetType(), m => m);
            _localModules = allModules.Where(m => !m.IsGlobal).ToDictionary(m => m.GetType(), m => m);
        }

        /// <summary>
        /// Return global module by type
        /// </summary>
        /// <returns>Global module or null</returns>
        internal T GetGlobalModule<T>() where T : EcsModule
        {
            return GetModule<T>(_globalModules);
        }
        
        /// <summary>
        /// Return module by type
        /// </summary>
        /// <returns>Module or null</returns>
        internal T GetModule<T>() where T : EcsModule
        {
            return GetModule<T>(_localModules);
        }

        private static T GetModule<T>(IReadOnlyDictionary<Type, EcsModule> modules) where T : EcsModule
        {
            var type = typeof(T);
            if (modules.ContainsKey(type))
                return modules[type] as T;
            return null;
        }

        private static IEnumerable<EcsModule> CreateAllEcsModules()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                            .SelectMany(a => a.GetTypes()
                                              .Where(t => t.IsSubclassOf(typeof(EcsModule)) && !t.IsAbstract)
                                              .Select(t => (EcsModule) Activator.CreateInstance(t)));
            ;
        }

        public bool IsModuleActive<TModule>() where TModule : EcsModule
        {
            return GetModule<TModule>().IsActive;
        }
    }
}