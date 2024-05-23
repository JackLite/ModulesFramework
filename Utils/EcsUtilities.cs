using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using ModulesFramework.Attributes;
using ModulesFramework.Data;
using ModulesFramework.Modules;
using ModulesFramework.Systems;

namespace ModulesFramework.Utils
{
    /// <summary>
    /// Static utilities
    /// </summary>
    internal static class EcsUtilities
    {
        /// <summary>
        ///     Returns all systems types from all assemblies.
        ///     It's a very heavy operation, so its result cached in the world
        /// </summary>
        public static Dictionary<Type, List<Type>> FindSystems(Func<Assembly, bool> filter)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(filter);
            var allSystems =
                from type in assemblies.SelectMany(a => a.GetTypes())
                where type.IsClass && typeof(ISystem).IsAssignableFrom(type)
                let attr = type.GetCustomAttribute<EcsSystemAttribute>()
                where attr != null
                select (type, attr.module);

            var result = new Dictionary<Type, List<Type>>();
            foreach (var (systemType, moduleType) in allSystems)
            {
                if (!result.TryGetValue(moduleType, out var systems))
                {
                    systems = new List<Type>();
                    result[moduleType] = systems;
                }

                systems.Add(systemType);
            }

            return result;
        }

        public static IEnumerable<Type> GetModulesTypes(Func<Assembly, bool> filter)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(filter);
            return assemblies.SelectMany(a => a.GetTypes()
                .Where(FilterModules)
                .Select(t => t));
        }

        private static bool FilterModules(Type type)
        {
            return
                type != typeof(EmbeddedGlobalModule)
                && type.IsSubclassOf(typeof(EcsModule))
                && !type.IsAbstract;
        }
    }
}