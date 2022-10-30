using ModulesFramework.Data;

namespace ModulesFramework
{
    internal class EcsOneFrameSystem
    {
        private DataWorld _world;
        public EcsOneFrameSystem(DataWorld world)
        {
            _world = world;
        }

        public void PostRun()
            {
                var entities = _world.Select<EcsOneFrame>().GetEntities();
                foreach (var e in entities)
                    e.Destroy();
            }
        }
}