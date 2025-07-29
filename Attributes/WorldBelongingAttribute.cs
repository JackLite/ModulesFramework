using System;
using System.Collections.Generic;

namespace ModulesFramework.Attributes
{
    /// <summary>
    ///     Marks that module belongs to a specified world.
    ///     If not specified, module belongs to all worlds
    ///     Be careful cause all systems in module will run once per world
    /// </summary>
    public class WorldBelongingAttribute : Attribute
    {
        public HashSet<string> Worlds { get; private set; }

        public WorldBelongingAttribute(params string[] worldIndex)
        {
            Worlds = new HashSet<string>(worldIndex);
        }
    }
}
