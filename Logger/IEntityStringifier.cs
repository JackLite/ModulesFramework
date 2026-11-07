using ModulesFramework.Data;

namespace ModulesFramework
{
    /// <summary>
    ///     Using for live entities when ToString() called
    /// </summary>
    public interface IEntityStringifier
    {
        public string ToString(Entity entity);
    }
}
