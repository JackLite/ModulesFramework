using System.Threading.Tasks;

namespace ModulesFramework.Modules
{
    public sealed class EmbeddedGlobalModule : EcsModule
    {
        protected override Task Setup()
        {
            world.RegisterEventSubscriber(this);
            return Task.CompletedTask;
        }
    }
}