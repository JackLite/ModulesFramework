using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ModulesFramework.Utils.Types
{
    internal static class TypeUtilities
    {
        public static IEnumerable<Type> GetTypes(Func<Assembly, bool> filter)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(filter);
            return from type in assemblies.SelectMany(a => a.GetTypes())
                where type.IsClass && !type.IsAbstract && type is { IsInterface: false, IsGenericType: false }
                select type;
        }
    }
}
