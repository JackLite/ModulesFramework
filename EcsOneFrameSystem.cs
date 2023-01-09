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
                using var select = _world.Select<EcsOneFrame>();
                var entities = select.GetEntities();
                foreach (var e in entities)
                    e.Destroy();
            }
        }
}