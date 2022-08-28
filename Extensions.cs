using System.Collections.Generic;
using System.Threading.Tasks;
using Core;

namespace EcsCore
{
    public static class Extensions
    {
        /// <summary>
        /// Activate module: call Setup() and GetDependencies()
        /// </summary>
        /// <param name="world"></param>
        /// <typeparam name="T">Type of module that you want to activate</typeparam>
        public static void ActivateModule<T>(this DataWorld world) where T : EcsModule
        {
            world.NewEntity()
                 .AddComponent(new ModuleActivationSignal
                 {
                     moduleType = typeof(T)
                 });
        }

        /// <summary>
        /// Activate module: call Setup() and GetDependencies()
        /// </summary>
        /// <param name="world"></param>
        /// <typeparam name="TModule">Type of module that you want to activate</typeparam>
        /// <typeparam name="TParent">Parent module. TModule get dependencies from parent</typeparam>
        public static void ActivateModule<TModule, TParent>(this DataWorld world) 
            where TModule : EcsModule
            where TParent : EcsModule
        {
            world.NewEntity()
                 .AddComponent(new ModuleActivationSignal
                 {
                     moduleType = typeof(TModule),
                     dependenciesModule = typeof(TParent)
                 });
        }

        /// <summary>
        /// Deactivate module: calls Deactivate() in module
        /// </summary>
        /// <param name="world"></param>
        /// <typeparam name="T">Type of module that you want to deactivate</typeparam>
        public static void DeactivateModule<T>(this DataWorld world) where T : EcsModule
        {
            world.NewEntity().AddComponent(new ModuleDeactivationSignal { ModuleType = typeof(T) });
        }

        /// <summary>
        /// Allow to create one frame entity. That entity will be destroyed after all run systems processed (include IEcsRunLate)
        /// WARNING: one frame creates immediately, but if some systems processed BEFORE creation one frame entity
        /// they WILL NOT processed that entity. You can create one frame in RunSystem and processed them in RunLateSystem.
        /// Also you can use GetSystemOrder() in your module for setting order of systems.
        /// </summary>
        /// <param name="world"></param>
        /// <returns>New entity</returns>
        /// <seealso cref="EcsModule.GetSystemsOrder"/>
        public static Entity CreateOneFrame(this DataWorld world)
        {
            return world.NewEntity().AddComponent(new EcsOneFrame());
        }

        public static void Forget(this Task task)
        {
            if (!task.IsCompleted || task.IsFaulted)
            {
                _ = ForgetAwaited(task);
            }

            static async Task ForgetAwaited(Task task)
            {
                await task.ConfigureAwait(false);
            }
        }

        #if !UNITY_2021_2_OR_NEWER
        public static void Deconstruct<T1, T2>(this KeyValuePair<T1, T2> pair, out T1 key, out T2 value)
        {
            key = pair.Key;
            value = pair.Value;
        }
        #endif
    }
}