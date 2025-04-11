using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ModulesFramework.Exceptions;

namespace ModulesFramework.Modules
{
    /// <summary>
    ///     Part of EcsModule controls submodules
    /// </summary>
    public partial class EcsModule
    {
        private readonly SortedDictionary<int, List<EcsModule>> _submodules
            = new SortedDictionary<int, List<EcsModule>>();

        public IEnumerable<EcsModule> Submodules => _submodules.Values.SelectMany(m => m);

        private async Task SetupSubmodules()
        {
            foreach (var submodules in _submodules.Values)
            {
                var tasks = new List<Task>();
                foreach (var submodule in submodules)
                {
                    if (submodule.IsInitWithParent)
                        tasks.Add(submodule.StartInit());
                }

                await Task.WhenAll(tasks);
            }
        }

        public virtual void AddSubmodule(EcsModule module)
        {
            var modulesOrder = GetSubmodulesOrder();
            modulesOrder.TryGetValue(module.ConcreteType, out int order);
            AddSubmodule(module, order);
        }

        public virtual void AddSubmodule(EcsModule module, int order)
        {
            if (!_submodules.ContainsKey(order))
                _submodules[order] = new List<EcsModule>();
            _submodules[order].Add(module);
        }

        /// <summary>
        ///     Change order of submodule update. This is a very rare need so think twice before use it
        /// </summary>
        /// <param name="module"></param>
        /// <param name="order"></param>
        public virtual void SetSubmoduleOrder(EcsModule module, int order)
        {
            var submoduleCheck = false;
            foreach (var (_, list) in _submodules)
            {
                if (!list.Contains(module))
                    continue;
                list.Remove(module);
                submoduleCheck = true;
                break;
            }

            if (!submoduleCheck)
            {
                throw new SubmoduleException(
                    $"Module {module.ConcreteType.Name} is not a submodule of {ConcreteType.Name}"
                );
            }

            AddSubmodule(module, order);
        }

        private void SetSubmodulesActive(bool isActive)
        {
            foreach (var submodules in _submodules.Values)
            {
                foreach (var submodule in submodules)
                {
                    if (submodule.IsActiveWithParent)
                        submodule.SetActive(isActive);
                }
            }
        }


        /// <summary>
        ///     Mark module as submodule of parent
        /// </summary>
        /// <param name="parent">Parent module</param>
        /// <param name="initWithParent">Mark that module must initialized with parent</param>
        /// <param name="activeWithParent">Mark that module must activated with parent</param>
        internal void MarkSubmodule(EcsModule parent, bool initWithParent, bool activeWithParent)
        {
            IsSubmodule = true;
            Parent = parent;
            IsInitWithParent = initWithParent;
            IsActiveWithParent = activeWithParent;
        }

        /// <summary>
        ///     Let you to set order of submodules. Default order is 0. Submodules will be ordered by ascending
        /// </summary>
        /// <returns>Key - type of submodule. Value - order.</returns>
        public virtual Dictionary<Type, int> GetSubmodulesOrder()
        {
            return new Dictionary<Type, int>(0);
        }
    }
}