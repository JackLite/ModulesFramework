using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ModulesFramework.Attributes;
using ModulesFramework.Systems;

namespace ModulesFramework.Modules
{
    [GlobalModule]
    internal sealed class EmbeddedGlobalModule : EcsModule
    {
        protected override IEnumerable<ISystem> GetSystems()
        {
            var types = GetSystemTypes().ToArray();
            foreach (var type in types)
            {
                if (type.GetInterfaces().All(t => t != typeof(ISystem)))
                    Console.WriteLine("[Error] Wrong type! " + type);
            }

            return types.Select(t => (ISystem)Activator.CreateInstance(t));
        }

        private static IEnumerable<Type> GetSystemTypes()
        {
            return
                from type in AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes())
                let attr = type.GetCustomAttribute<GlobalSystemAttribute>()
                where attr != null
                select type;
        }
    }
}