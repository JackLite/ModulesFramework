﻿using System;
using ModulesFramework.Modules;

namespace ModulesFramework.Exceptions
{
    public sealed class ModuleNotFoundException : Exception
    {
        public ModuleNotFoundException(Type moduleType) : base("Can't find module with type " + moduleType)
        {
        }
    }
}