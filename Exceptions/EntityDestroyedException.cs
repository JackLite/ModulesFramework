using System;

namespace ModulesFramework.Exceptions
{
    public class EntityDestroyedException : Exception
    {
        public EntityDestroyedException(int entityId) : base($"Entity {entityId} is destroyed.")
        {
        }
    }
}