using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ModulesFramework.Exceptions;
using ModulesFramework.Utils.Types;

namespace ModulesFramework.Modules
{
    /// <summary>
    ///     Part of EcsModule controls submodules
    /// </summary>
    public partial class EcsModule
    {
        private struct SubmodulesGroup
        {
            public int order;
            public List<EcsModule> modules;
        }
        
        private readonly HashSet<int> _orders = new HashSet<int>();
        
        private readonly LinkedList<SubmodulesGroup> _submodulesGroups = new LinkedList<SubmodulesGroup>();

        public IEnumerable<EcsModule> Submodules => _submodulesGroups.SelectMany(g => g.modules);

        private async Task SetupSubmodules()
        {
            foreach (var group in _submodulesGroups)
            {
                var tasks = new List<Task>();
                foreach (var submodule in group.modules)
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
            if (!_orders.Add(order))
            {
                foreach (var group in _submodulesGroups)
                {
                    if (group.order == order)
                    {
                        group.modules.Add(module);
                    }
                }
            }
            else
            {
                var group = new SubmodulesGroup
                {
                    order = order,
                    modules = new List<EcsModule>
                    {
                        module
                    }
                };

                if (_submodulesGroups.Count == 0)
                {
                    _submodulesGroups.AddLast(group);
                    return;
                }
                
                var node = _submodulesGroups.First;
                while (node.Value.order < order)
                {
                    if (node.Next == null)
                    {
                        _submodulesGroups.AddLast(group);
                        return;
                    }
                    node = node.Next;
                }
                _submodulesGroups.AddBefore(node, group);
            }
        }

        /// <summary>
        ///     Change order of submodule update. This is a very rare need so think twice before use it
        /// </summary>
        /// <param name="module"></param>
        /// <param name="order"></param>
        public virtual void SetSubmoduleOrder(EcsModule module, int order)
        {
            var submoduleCheck = false;
            foreach (var group in _submodulesGroups)
            {
                if (!group.modules.Contains(module))
                    continue;
                group.modules.Remove(module);
                submoduleCheck = true;
                break;
            }

            if (!submoduleCheck)
            {
                throw new SubmoduleException(
                    $"Module {module.ConcreteType.GetTypeName()} is not a submodule of {ConcreteType.GetTypeName()}"
                );
            }

            AddSubmodule(module, order);
        }

        private void SetSubmodulesActive(bool isActive)
        {
            foreach (var group in _submodulesGroups)
            {
                foreach (var submodule in group.modules)
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