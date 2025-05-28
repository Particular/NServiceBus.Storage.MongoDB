namespace NServiceBus.Storage.MongoDB.Tests;

using System;

public static class EqualityExtensions
{
    public static bool EqualTo<T>(this T item, object obj, Func<T, T, bool> equals) where T : class
    {
        if (ReferenceEquals(item, obj))
        {
            return true;
        }

        if (item == null || obj is not T x)
        {
            return false;
        }

        return equals(item, x);
    }
}