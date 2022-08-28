using System;

namespace EcsCore.DependencyInjection
{
    [AttributeUsage(AttributeTargets.Method)]
    public class SetupAttribute : Attribute
    {
    }
}