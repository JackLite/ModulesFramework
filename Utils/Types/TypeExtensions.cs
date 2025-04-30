using System;
using System.Linq;

namespace ModulesFramework.Utils.Types
{
    public static class TypeExtensions
    {
        public static string GetTypeName(this Type type)
        {
            if (!type.IsGenericType)
                return type.Name;

            var result = type.Name.Substring(0, type.Name.Length - 2);
            result += $"<{string.Join(", ", type.GenericTypeArguments.Select(t => t.GetTypeName()))}>";
            return result;
        }
    }
}