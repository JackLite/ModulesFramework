using System;
using System.Runtime.CompilerServices;

namespace ModulesFramework.Data
{
    /// <summary>
    ///     Part of world to control global dependencies
    /// </summary>
    public partial class DataWorld
    {
        private Func<Type, object?> _getGlobalDependenciesFunc = delegate
        {
            return null;
        };

        /// <summary>
        ///     Allows to set custom dependencies resolver <br/>
        ///     Dependencies that resolves that way available in any module and thus in any system<br/>
        ///     <b>Important:</b> you should use this method as soon as possible, ideally when creating world
        ///     and before it starts
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetDependenciesGetter(Func<Type, object?> getter)
        {
            _getGlobalDependenciesFunc = getter;
        }

        /// <summary>
        ///     Returns global dependency by type. It returns null if no getter is set for
        ///     global dependencies
        ///     <seealso cref="SetDependenciesGetter"/>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object? GetGlobalDependency(Type type)
        {
            return _getGlobalDependenciesFunc(type);
        }
    }
}