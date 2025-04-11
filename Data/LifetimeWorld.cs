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

        public void Run()
        {
            _embeddedGlobalModule.Run();
            foreach (var module in _modules.Values)
            {
                if (module.IsRoot)
                    module.Run();
            }
        }

        public void RunPhysic()
        {
            _embeddedGlobalModule.RunPhysics();
            foreach (var module in _modules.Values)
            {
                if (module.IsRoot)
                    module.RunPhysics();
            }
        }

        public void PostRun()
        {
            _embeddedGlobalModule.PostRun();
            foreach (var module in _modules.Values)
            {
                if (module.IsRoot)
                    module.PostRun();
            }
        }

        public void FrameEnd()
        {
            _embeddedGlobalModule.FrameEnd();
            foreach (var module in _modules.Values)
            {
                if (module.IsRoot)
                    module.FrameEnd();
            }
        }
    }
}