using System.Collections.Generic;
using System.Threading.Tasks;
using ModulesFramework.Data;
using ModulesFramework.Modules;
using ModulesFramework.Modules.Components;

namespace ModulesFramework
{
    public static class Extensions
    {
        

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