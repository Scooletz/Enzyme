using System.Collections.Generic;

namespace Enzyme
{
    static class CollectionExtensions
    {
        public static TValue GetOrCreate<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key)
            where TValue : new()
        {
            if (dictionary.TryGetValue(key, out var value) == false)
            {
                value = new TValue();
                dictionary[key] = value;
            }

            return value;
        }

#if NETSTANDARD
        public static bool TryPop<TValue>(this Stack<TValue> stack, out TValue value)
        {
            if (stack.Count == 0)
            {
                value = default;
                return false;
            }

            value = stack.Pop();
            return true;
        }
# endif
    }
}