using System;
using System.Collections.Generic;

namespace ModulesFramework.Data.QueryUtils
{
    public class Filter
    {
        public static OrBuilder Or<T>() where T : struct
        {
            var builder = new OrBuilder
            {
                orWrappers = new Dictionary<Type, OrWrapper>
                {
                    {
                        typeof(T), new OrWrapper<T>()
                    }
                }
            };
            return builder;
        }

        public static WhereOrBuilder Or<T>(Func<T, bool> customFilter) where T : struct
        {
            var builder = new WhereOrBuilder
            {
                wrappers = new Dictionary<Type, WhereOrWrapper>
                {
                    {
                        typeof(T), new WhereOrWrapper<T>(customFilter)
                    }
                }
            };
            return builder;
        }
    }
}