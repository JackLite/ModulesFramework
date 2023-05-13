using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ModulesFramework.Data
{
    public struct Entity
    {
        public int generation;
        public int Id { get; set; }
        public DataWorld World { get; set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Entity AddComponent<T>(T component) where T : struct
        {
            World.AddComponent(Id, component);
            return this;
        }

        public Entity AddNewComponent<T>(T component) where T : struct
        {
            World.AddNewComponent(Id, component);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetComponent<T>() where T : struct
        {
            return ref World.GetComponent<T>(Id);
        }

        public Span<int> GetIndices<T>() where T : struct
        {
            return World.GetIndices<T>(Id);
        }

        public ref T GetComponentAt<T>(int index) where T : struct
        {
            return ref World.GetComponentAt<T>(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Entity RemoveComponent<T>() where T : struct
        {
            World.RemoveComponent<T>(Id);
            return this;
        }

        public Entity RemoveFirstComponent<T>() where T : struct
        {
            World.RemoveFirstComponent<T>(Id);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Destroy()
        {
            World.DestroyEntity(Id);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasComponent<T>() where T : struct
        {
            return World.HasComponent<T>(Id);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsAlive()
        {
            #if !MODULES_OPT
            if (World == null)
                return false;
            #endif
            return World.IsEntityAlive(this);
        }
    }
}