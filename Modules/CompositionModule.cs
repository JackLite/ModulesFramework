using System.Collections.Generic;
using System.Threading.Tasks;

namespace ModulesFramework.Modules
{
    /// <summary>
    ///     Part of EcsModule controls composition of modules
    /// </summary>
    public partial class EcsModule
    {
        private readonly List<EcsModule> _composedModules = new List<EcsModule>();
        private readonly List<Task> _tasksCache = new List<Task>();

        public IEnumerable<EcsModule> ComposedModules => _composedModules;

        public void AddComposedModule(EcsModule module)
        {
            _composedModules.Add(module);
        }

        private async Task SetupComposition()
        {
            _tasksCache.Clear();
            foreach (var composedModule in _composedModules)
            {
                _tasksCache.Add(composedModule.SetupSelfAndSubmodules());
            }

            await Task.WhenAll(_tasksCache);
            
            foreach (var composedModule in _composedModules)
                composedModule.InsertDependencies();

            _tasksCache.Clear();
            foreach (var composedModule in _composedModules)
            {
                _tasksCache.Add(composedModule.OnSetupEndSelfAndSubmodules());
            }

            await Task.WhenAll(_tasksCache);
        }

        private void SetActiveComposition(bool isActive)
        {
            foreach (var composedModule in _composedModules)
            {
                composedModule.SetActive(isActive);
            }
        }
    }
}