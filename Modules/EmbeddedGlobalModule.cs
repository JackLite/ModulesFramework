using System.Threading.Tasks;
using ModulesFramework.Attributes;

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