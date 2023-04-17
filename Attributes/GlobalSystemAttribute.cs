using System;

namespace ModulesFramework.Attributes
{
    /// <summary>
    ///     Marks that system is global so it always running and can't have dependencies except world.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class GlobalSystemAttribute : Attribute
    {
    }
}