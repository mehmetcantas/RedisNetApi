using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using RedisNetApi.Caching;

namespace RedisNetApi.Attributes
{
    // Bu attribute'un yalnızca sınıf ve metotlar ile kullanılabileceğini belirttik.
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class CachedAttribute : Attribute, IAsyncActionFilter
    {
        private readonly int _expireTimeSeconds;

        // Attribute kullanılırken parametre olarak aldığımız saniye türündeki expire süresini yukarıda tanımlanan değişkene atıyoruz.
        public CachedAttribute(int expireTimeSeconds)
        {
            _expireTimeSeconds = expireTimeSeconds;
        }

        // Kullanıcıdan istek geldiği anda
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // IResponseCacheFactory interface'ini inject ediyoruz.
            var cacheFactory = context.HttpContext.RequestServices.GetRequiredService<IResponseCacheFactory>();
            // Aşağıda yazdığımız cache key yaratan metotumuzu burada kullanarak yaratılan cache key değerini bir değişkene atıyoruz.
            var cacheKey = GenerateCacheKeyFromRequest(context.HttpContext.Request);
            // oluşan cache key'in işaret ettiği veriyi alabilmek için IResponseCacheFactory içerisindeki GetCachedeResponseAsync ismindeki asenkron metotumuzu çağırıyoruz.
            var cachedResponse =  await cacheFactory.GetCachedResponseAsync(cacheKey);

            // eğer cache'lenmiş bir veri varsa
            if (!string.IsNullOrEmpty(cachedResponse))
            {   
                // 200 kodunda bir cevap ile birlikte verimizi gönderiyoruz.
                var contentResult = new ContentResult
                {
                    Content = cachedResponse,
                    ContentType = "application/json",
                    StatusCode = 200
                };
                context.Result = contentResult;
                return;
            }
            // Eğer yoksa isteği devam ettiriyoruz.
            var executedContext = await next();

            // Burada ise eğer sonuç OkObjectResult ise verimizi cache'liyoruz. 400,401,403 vb. kodlarda cache'leme işlemi yapılmayacaktır.
            if (executedContext.Result is OkObjectResult okObjectResult)
            {
                 await cacheFactory.CacheResponseAsync(cacheKey, okObjectResult.Value,
                    TimeSpan.FromSeconds(_expireTimeSeconds));
            }
        }
        // Gelen isteğin path değerini cache key olarak kullanıyoruz örn: /api/home/getall ya da /api/article/1
        private static string GenerateCacheKeyFromRequest(HttpRequest httpContextRequest)
        {
            var keyBuilder = new StringBuilder();
            keyBuilder.Append($"{httpContextRequest.Path}");

            foreach (var (key, value) in httpContextRequest.Query.OrderBy(x => x.Key))
            {
                keyBuilder.Append($"|{key}-{value}");
            }

            return keyBuilder.ToString();
        }
    }
}