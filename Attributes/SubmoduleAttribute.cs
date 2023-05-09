using System;
using ModulesFramework.Exceptions;

namespace ModulesFramework.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class SubmoduleAttribute : Attribute
    {
        public readonly Type parent;
        public readonly bool initWithParent;
        public readonly bool activeWithParent;

        public SubmoduleAttribute(Type parent, bool initWithParent = true, bool activeWithParent = true)
        {
            this.parent = parent;
            this.initWithParent = initWithParent;
            this.activeWithParent = activeWithParent;
            if (!this.initWithParent && this.activeWithParent)
            {
                throw new SubmoduleException(
                    $"Submodule can't be activated with parent, but not initialized with parent {parent.Name}"
                );
            }
        }
    }
}