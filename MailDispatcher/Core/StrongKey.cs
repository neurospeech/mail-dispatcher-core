#nullable enable
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace MailDispatcher.Core
{
    public static class StrongCacheExtensions
    {
        public static StrongCache<T> CreateStrongCache<T>(this IMemoryCache cache)
        {
            return new StrongCache<T>(cache);
        }

    }
    public class StrongCache<T>
    {

        private readonly IMemoryCache cache;

        public StrongCache(IMemoryCache cache)
        {
            this.cache = cache;
        }

        public (Type, object) ToKey(object key) => (typeof(T), key);

        public T Set(object key, T value, DateTimeOffset dateTimeOffset)
        {
            return cache.Set(ToKey(key), value, dateTimeOffset);
        }

        public T Set(object key, T value, TimeSpan timeSpan)
        {
            return cache.Set(ToKey(key), value, timeSpan);
        }

        public void Remove(object key)
        {
            cache.Remove(ToKey(key));
        }

        public T GetOrCreate(object key, Func<object, T> factory)
        {
            return cache.GetOrCreate(ToKey(key), factory);
        }

        public bool TryGetValue(
            object key,
            [NotNullWhen(true)]
                out T value)
        {
            if (cache.TryGetValue(ToKey(key), out var v))
            {
                value = (T)v;
                return true;
            }
            value = default!;
            return false;
        }
    }
}
