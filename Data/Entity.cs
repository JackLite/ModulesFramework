using System.Runtime.CompilerServices;

namespace ModulesFramework.Data
{
    public struct Entity
    {
        public int Id { get; set; }
        public DataWorld World { get; set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Entity AddComponent<T>(T component) where T : struct
        {
            World.AddComponent(Id, component);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetComponent<T>() where T : struct
        {
            return ref World.GetComponent<T>(Id);
        }

        public Entity RemoveComponent<T>() where T : struct
        {
            World.RemoveComponent<T>(Id);
            return this;
        }

        public void Destroy()
        {
            World.DestroyEntity(Id);
        }
    }
}