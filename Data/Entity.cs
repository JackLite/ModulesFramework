using System;
using System.Runtime.CompilerServices;
using ModulesFramework.Data.Enumerators;

namespace ModulesFramework.Data
{
    public struct Entity
    {
        public int generation;
        public int Id { get; set; }
        public DataWorld World { get; set; }

        /// <summary>
        ///     Add component to entity
        ///     If component exists it will be replaced
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Entity AddComponent<T>(T component) where T : struct
        {
            World.AddComponent(Id, component);
            return this;
        }
        
        /// <summary>
        ///     Add new multiple component T to entity
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Entity AddNewComponent<T>(T component) where T : struct
        {
            World.AddNewComponent(Id, component);
            return this;
        }

        /// <summary>
        ///     Return component T from entity
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetComponent<T>() where T : struct
        {
            return ref World.GetComponent<T>(Id);
        }

        /// <summary>
        ///     Return enumerable of inner indices for multiple components
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public  MultipleComponentsIndicesEnumerable<T> GetIndices<T>() where T : struct
        {
            return World.GetIndices<T>(Id);
        }

        /// <summary>
        ///     Get multiple component T from entity by internal index 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetComponentAt<T>(int mtmIndex) where T : struct
        {
            return ref World.GetComponentAt<T>(Id, mtmIndex);
        }

        /// <summary>
        ///     Return all T components from entity
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MultipleComponentsEnumerable<T> GetAll<T>() where T : struct
        {
            return World.GetAllComponents<T>(Id);
        }

        /// <summary>
        ///     Remove T component from entity 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Entity RemoveComponent<T>() where T : struct
        {
            World.RemoveComponent<T>(Id);
            return this;
        }

        /// <summary>
        ///     Remove first multiple component from entity
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Entity RemoveFirstComponent<T>() where T : struct
        {
            World.RemoveFirstComponent<T>(Id);
            return this;
        }

        /// <summary>
        ///     Remove all T multiple components from entity
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Entity RemoveAll<T>() where T : struct
        {
            World.RemoveAll<T>(Id);
            return this;
        }

        /// <summary>
        ///     Destroy entity and removes all it's components from tables
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Destroy()
        {
            World.DestroyEntity(Id);
        }

        /// <summary>
        ///     Return true if entity has component
        ///     Note: it works the same way for multiple components
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasComponent<T>() where T : struct
        {
            return World.HasComponent<T>(Id);
        }

        /// <summary>
        ///     Return true if entity was not destroyed
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsAlive()
        {
            #if !MODULES_OPT
            if (World == null)
                return false;
            #endif
            return World.IsEntityAlive(this);
        }

        /// <summary>
        ///     Returns count of T components on entity
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Count<T>() where T : struct
        {
            return World.CountComponentsAt<T>(Id);
        }
    }
}