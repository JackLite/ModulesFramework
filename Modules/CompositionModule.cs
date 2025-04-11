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

        public void AddComposedModule(EcsModule module)
        {
            _composedModules.Add(module);
        }

        private async Task SetupComposition()
        {
            _tasksCache.Clear();
            foreach (var composedModule in _composedModules)
            {
                _tasksCache.Add(composedModule.StartInit());
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