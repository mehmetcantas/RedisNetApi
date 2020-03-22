using System;
using System.Threading.Tasks;

namespace RedisNetApi.Caching
{
   public interface IResponseCacheFactory
    {
        Task CacheResponseAsync(string cacheKey, object response, TimeSpan expireTimeSeconds);
        Task<string> GetCachedResponseAsync(string cacheKey);
    }
}