namespace ModulesFramework.Modules
{
    // IMPORTANT. all of this is under development
    /// <summary>
    ///     Describes current state of module
    /// </summary>
    public interface IModuleState
    {
        public EcsModule Module { get; }
    }
    
    public abstract class BaseModuleState : IModuleState
    {
        public EcsModule Module { get; }

        protected BaseModuleState(EcsModule module)
        {
            Module = module;
        }
    }
    
    /// <summary>
    ///     Default and first state of module
    /// </summary>
    public class ModuleCreatedState : BaseModuleState
    {
        public ModuleCreatedState(EcsModule module) : base(module)
        {
        }
    }
    
    /// <summary>
    ///     Module is initialized and ready to be activated
    /// </summary>
    public class ModuleInitializedState : BaseModuleState
    {
        public ModuleInitializedState(EcsModule module) : base(module)
        {
        }
    }
    
    /// <summary>
    ///     Module is active and running
    /// </summary>
    public class ModuleRunningState : BaseModuleState
    {
        public ModuleRunningState(EcsModule module) : base(module)
        {
        }
    }
}