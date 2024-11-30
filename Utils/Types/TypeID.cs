using System.Collections.Generic;

namespace ModulesFramework.Utils.Types
{
    /// <summary>
    ///     Hack to convert type to int to fasten getting things by type
    ///     TBaseType is a type of thing in collection
    /// </summary>
    internal class TypeID<TBaseType, TType> : TypeID<TBaseType>
    {
        public static int id = -1;

        public override void Reset()
        {
            id = -1;
        }
    }

    internal static class TypeIDCollection<TBaseType>
    {
        private static List<TypeID<TBaseType>> types = new List<TypeID<TBaseType>>();

        public static int Add<TType>()
        {
            TypeID<TBaseType, TType>.id = types.Count;
            types.Add(new TypeID<TBaseType, TType>());
            return TypeID<TBaseType, TType>.id;
        }

        public static void Reset()
        {
            foreach (var typeId in types)
                typeId.Reset();
        }
    }

    internal abstract class TypeID<TBaseType>
    {
        public abstract void Reset();
    }
}
