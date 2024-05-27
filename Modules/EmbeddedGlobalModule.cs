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
            var systems = new List<ISystem>(types.Length);
            foreach (var type in types)
            {
                try
                {
                    var system = (ISystem)Activator.CreateInstance(type);
                    systems.Add(system);
                }
                catch (InvalidCastException)
                {
                    world.Logger.LogError($"System {type} should implement {nameof(ISystem)} interface");
                }
            }

            return systems;
        }

        private static IEnumerable<Type> GetSystemTypes()
        {
            return
                from type in AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes())
                where type.IsAssignableFrom(typeof(ISystem))
                let attr = type.GetCustomAttribute<GlobalSystemAttribute>()
                where attr != null
                select type;
        }
    }
}