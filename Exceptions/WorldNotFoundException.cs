using System;

namespace ModulesFramework.Exceptions
{
    /// <summary>
    ///     Throws when user tries to get world that doesn't exist
    /// </summary>
    public class WorldNotFoundException : Exception
    {
        public WorldNotFoundException(string worldName) : base($"World with name {worldName} not found") { }
        public WorldNotFoundException(int worldIndex) : base($"World with index {worldIndex} not found") { }
    }
}
