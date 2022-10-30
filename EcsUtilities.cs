using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ModulesFramework.Attributes;
using ModulesFramework.Modules;
using ModulesFramework.Systems;

namespace ModulesFramework
{
    /// <summary>
    /// Static utilities
    /// </summary>
    internal static class EcsUtilities
    {
        /// <summary>
        /// Create systems for module
        /// </summary>
        /// <param name="moduleType">Type of module</param>
        /// <returns>Enumerable of created systems</returns>
        /// <seealso cref="EcsModule.Init"/>
        internal static IEnumerable<ISystem> CreateSystems(Type moduleType)
        {
            var types = GetSystemTypes(moduleType).ToArray();
            foreach (var type in types)
            {
                if (type.GetInterfaces().All(t => t != typeof(ISystem)))
                    Console.WriteLine("[Error] Wrong type! " + type);
            }

            return types.Select(t => (ISystem)Activator.CreateInstance(t));
        }

        private static IEnumerable<Type> GetSystemTypes(Type moduleType)
        {
            return
                from type in AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes())
                let attr = type.GetCustomAttribute<EcsSystemAttribute>()
                where attr != null && attr.module == moduleType
                select type;
        }
    }
}