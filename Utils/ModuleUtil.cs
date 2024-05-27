using System;
using System.Collections.Generic;
using System.Reflection;
using ModulesFramework.Attributes;

namespace ModulesFramework.Utils
{
    public static class ModuleUtil
    {
        private static readonly HashSet<int> _defaultWorlds = new HashSet<int> { 0 };
        
        public static HashSet<int> GetWorldIndex(Type moduleType)
        {
            var worldAttribute = moduleType.GetCustomAttribute<WorldBelongingAttribute>();
            return worldAttribute != null ? worldAttribute.WorldIndices : _defaultWorlds;
        }
    }
}