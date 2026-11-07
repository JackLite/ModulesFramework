using ModulesFramework.Data;

namespace ModulesFramework
{
    public class DefaultEntityStringifier : IEntityStringifier
    {

        public string ToString(Entity entity)
        {
            return $"Entity {entity.GetCustomId()}";
        }
    }
}
