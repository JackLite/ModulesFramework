namespace ModulesFramework.Data.QueryUtils
{
    internal class OrWrapper<T> : OrWrapper where T : struct
    {
        internal override bool HasComponent(int eid, DataWorld world)
        {
            return world.HasComponent<T>(eid);
        }
    }

    internal abstract class OrWrapper
    {
        internal abstract bool HasComponent(int eid, DataWorld world);
    }
}