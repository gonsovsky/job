using System;
using System.Collections.Generic;

namespace Coral.Atoll.Utils
{
    public static class DictionaryExtensions
    {

        public static U GetOrDefault<T, U>(this IDictionary<T, U> dict, T key) where U : class
        {
            U val;
            dict.TryGetValue(key, out val);
            return val;
        }

        public static TR GetOrDefault<T, U, TR>(this IDictionary<T, U> dict, T key, Func<U, TR> transformation)
            where U : class
        {
            U val;
            dict.TryGetValue(key, out val);
            TR res = transformation(val);
            return res;
        }
    }
}
