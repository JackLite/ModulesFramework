using System.Reflection;

namespace ModulesFramework.Utils
{
    /// <summary>
    ///     Filter for assemblies so you can look modules and systems only inside your project
    /// </summary>
    public class AssemblyFilter
    {
        public virtual bool Filter(Assembly assembly)
        {
            return
                assembly.FullName != null
                && !assembly.FullName.Contains("mscorlib")
                && assembly.FullName != "System"
                && !assembly.FullName.StartsWith("System.");
        }
    }
}