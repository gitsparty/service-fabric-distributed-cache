using System;
using Microsoft.Extensions.Caching.Distributed;

namespace SoCreate.ServiceFabric.DistributedCache
{
    public static class SharedWithClients
    {
        public static (DateTimeOffset?, TimeSpan?) ValidateAndGetAbsoluteAndSlidingExpirations(
            DateTimeOffset utcNow,
            DistributedCacheEntryOptions options)
        {
            var absoluteExpiration = GetAbsoluteExpiration(utcNow, options);
            var slidingExpiration = options.SlidingExpiration;

            if (slidingExpiration.HasValue)
            {
                absoluteExpiration = utcNow.AddMilliseconds(slidingExpiration.Value.TotalMilliseconds);
            }

            ValidateOptions(absoluteExpiration, slidingExpiration);

            return (absoluteExpiration, slidingExpiration);
        }

        public static DateTimeOffset? GetAbsoluteExpiration(DateTimeOffset utcNow, DistributedCacheEntryOptions options)
        {
            var expireTime = new DateTimeOffset?();
            if (options.AbsoluteExpirationRelativeToNow.HasValue)
            {
                expireTime = new DateTimeOffset?(utcNow.Add(options.AbsoluteExpirationRelativeToNow.Value));
            }
            else if (options.AbsoluteExpiration.HasValue)
            {
                if (options.AbsoluteExpiration.Value <= utcNow)
                {
                    throw new InvalidOperationException("The absolute expiration value must be in the future.");
                }
                expireTime = new DateTimeOffset?(options.AbsoluteExpiration.Value);
            }

            return expireTime;
        }

        public static void ValidateOptions(DateTimeOffset? absoluteExpiration, TimeSpan? slidingExpiration)
        {
            if (!slidingExpiration.HasValue && !absoluteExpiration.HasValue)
            {
                throw new InvalidOperationException("Either absolute or sliding expiration needs to be provided.");
            }
        }
    }
}
