using System;
using ModulesFramework.Modules;

namespace ModulesFramework.Attributes
{
    /// <summary>
    /// Mark system for auto-creation in concrete module
    /// </summary>
    /// <seealso cref="EcsModule"/>
    public class EcsSystemAttribute : Attribute
    {
        /// <summary>
        /// Type of module
        /// </summary>
        public readonly Type module;

        /// <param name="module">Type of module</param>
        /// <seealso cref="EcsModule"/>
        /// <seealso cref="EcsUtilities"/>
        public EcsSystemAttribute(Type module)
        {
            this.module = module;
        }
    }
}