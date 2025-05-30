using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ModulesFramework.Data
{
    /// <summary>
    ///     Part of world that contains modules lifetime logic
    /// </summary>
    public partial class DataWorld
    {
        /// <summary>
        ///     Starts the world. It inits global systems and global modules
        /// </summary>
        public async Task Start()
        {
            await _embeddedGlobalModule.Init(true);
            try
            {
                var tasks = new List<Task>();
                foreach (var module in _modules.Values)
                {
                    if (module.IsGlobal)
                    {
                        tasks.Add(module.Init(true));
                    }
                }

                await Task.WhenAll(tasks);
            }
            catch (Exception e)
            {
                Logger.RethrowException(e);
                throw;
            }
        }

        /// <summary>
        ///     Update tick (IRunSystem and IRunEventSystem)
        /// </summary>
        public void Run()
        {
            _embeddedGlobalModule.Run();
            foreach (var module in _modules.Values)
            {
                if (module.IsRoot)
                    module.Run();
            }
        }

        /// <summary>
        ///     Physic tick (IRunPhysicSystem) 
        /// </summary>
        public void RunPhysic()
        {
            _embeddedGlobalModule.RunPhysics();
            foreach (var module in _modules.Values)
            {
                if (module.IsRoot)
                    module.RunPhysics();
            }
        }

        /// <summary>
        ///     Post or late update tick (IPostRunSystem and IPostRunEventSystem)
        /// </summary>
        public void PostRun()
        {
            _embeddedGlobalModule.PostRun();
            foreach (var module in _modules.Values)
            {
                if (module.IsRoot)
                    module.PostRun();
            }
        }

        /// <summary>
        ///     Frame end tick (IFrameEndEventSystem)
        /// </summary>
        public void FrameEnd()
        {
            _embeddedGlobalModule.FrameEnd();
            foreach (var module in _modules.Values)
            {
                if (module.IsRoot)
                    module.FrameEnd();
            }
        }
        
        /// <summary>
        ///     Deactivate and destroys all modules, clear all OneData, components and entities<br/>
        ///     <b>Important:</b> it's not guaranteed that you can start same world again
        /// </summary>
        internal void Destroy()
        {
            foreach (var module in _modules.Values)
            {
                if (module.IsActive)
                    module.SetActive(false);
            }

            foreach (var module in _modules.Values)
            {
                if (module.IsInitialized)
                    module.Destroy();
            }

            _embeddedGlobalModule.Destroy();

            foreach (var table in _data.Values)
                table.ClearTable();

            _entitiesTable.ClearTable();
            _oneDatas.Clear();
            _entityCount = 0;
        }
    }
}