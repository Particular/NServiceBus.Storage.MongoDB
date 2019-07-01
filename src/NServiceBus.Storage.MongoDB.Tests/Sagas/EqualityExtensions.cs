using System;

namespace NServiceBus.Storage.MongoDB.Tests
{
    public static class EqualityExtensions
    {
        public static bool EqualTo<T>(this T item, object obj, Func<T, T, bool> equals) where T : class
        {
            var x = obj as T;

            if (ReferenceEquals(item, obj))
            {
                return true;
            }

            if (item == null || x == null)
            {
                return false;
            }

            return equals(item, x);
        }
    }
}