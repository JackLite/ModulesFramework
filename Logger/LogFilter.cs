using System;

namespace ModulesFramework
{
    /// <summary>
    /// Filter for logs
    /// </summary>
    [Flags]
    public enum LogFilter
    {
        None = 0,
        ModulesFull = 1,
        EntityLife = 1 << 1,
        EntityModifications = 1 << 2,
        EntityFull = EntityLife | EntityModifications,
        EventsFull = 1 << 3,
        SystemsInit = 1 << 4,
        SystemsDestroy = 1 << 5,
        SystemsLifetime = SystemsInit | SystemsDestroy,
        OneDataFull = 1 << 6,
        Performance = 1 << 7,
        Full = ModulesFull | EntityFull | EventsFull | SystemsLifetime | OneDataFull | Performance
    }
}