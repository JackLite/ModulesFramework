using System;
using System.Collections.Generic;
using System.Linq;
using ModulesFramework.Utils;

namespace ModulesFramework
{
    /// <summary>
    ///     Caches all found systems and modules
    /// </summary>
    internal class MFCache
    {
        private Dictionary<Type, List<Type>> _allSystems;
        private List<Type> _allModules;

        public Dictionary<Type, List<Type>> AllSystemTypes => _allSystems;
        public List<Type> AllModuleTypes => _allModules;

        public MFCache(AssemblyFilter assemblyFilter)
        {
            _allSystems = EcsUtilities.FindSystems(assemblyFilter.Filter);
            _allModules = EcsUtilities.GetModulesTypes(assemblyFilter.Filter).ToList();
        }
    }
}
