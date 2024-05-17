using System;
using System.Collections.Generic;

namespace ModulesFramework.Attributes
{
    /// <summary>
    ///     Marks that world belongs to specified world
    ///     Be careful cause all systems in module will run once per world
    ///     Note: probably you will never need this, but for some complex multiplayer games it will be
    ///     necessary in host mode
    /// </summary>
    public class WorldBelongingAttribute : Attribute
    {
        public HashSet<int> WorldIndices { get; private set; }

        public WorldBelongingAttribute(params int[] worldIndex)
        {
            WorldIndices = new HashSet<int>(worldIndex);
        }
    }
}