using System.Threading.Tasks;
using ModulesFramework.Attributes;

namespace ModulesFramework.Modules
{
    internal sealed class EmbeddedGlobalModule : EcsModule
    {
        protected override Task Setup()
        {
            world.RegisterEventSubscriber(this);
            return Task.CompletedTask;
        }
    }
}