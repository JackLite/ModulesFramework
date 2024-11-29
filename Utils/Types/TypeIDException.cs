using System;

namespace ModulesFramework.Utils.Types
{
    internal class TypeIDException : Exception
    {
        public TypeIDException(Type collectionType, Type elementType)
            : base($"Type id for {elementType} already created (collection type is {collectionType})")
        {

        }
    }

}
