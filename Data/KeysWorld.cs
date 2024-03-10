using System;
using System.Runtime.CompilerServices;

namespace ModulesFramework.Data
{
    public partial class DataWorld
    {
        /// <summary>
        ///     Create custom key for component, so you can get component or entity by component's key
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CreateKey<T, TKey>(Func<T, TKey> keyGetter) where T : struct where TKey : notnull
        {
            GetEcsTable<T>().CreateKey(keyGetter);
        }

        /// <summary>
        ///     Return component by custom key. Throws exception if there is no key or component
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T ComponentByKey<T, TKey>(TKey key) where T : struct where TKey : notnull
        {
            return ref GetEcsTable<T>().ByKey(key);
        }

        /// <summary>
        ///     Check if there is component with given key
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsKeyExists<T, TKey>(TKey key) where T : struct where TKey : notnull
        {
            return GetEcsTable<T>().HasKey(key);
        }

        /// <summary>
        ///     Return entity by custom key on some component on entity.
        ///     Returns nul if there is no entity found
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Entity FindEntityByKey<T, TKey>(TKey key) where T : struct where TKey : notnull
        {
            var eid = GetEcsTable<T>().FindEidByKey(key);
            return eid < 0 ? default : GetEntity(eid);
        }

        /// <summary>
        ///     Updates custom key for component. It's remove old key first and then create new one
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateKey<T, TKey>(TKey oldKey, T component, int eid)
            where T : struct
            where TKey : notnull
        {
            GetEcsTable<T>().UpdateKey(oldKey, component, eid);
        }
    }
}