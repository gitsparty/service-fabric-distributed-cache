using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using SoCreate.ServiceFabric.DistributedCache.Abstractions;

namespace SoCreate.ServiceFabric.DistributedCache.StatelessService
{
    [Route("api/[controller]")]
    [ApiController]
    public class CacheDemoController : ControllerBase
    {
        private readonly IDistributedCacheWithCreate _distributedCacheClient;

        public CacheDemoController(
            IDistributedCacheWithCreate distributedCacheClient)
        {
            _distributedCacheClient = distributedCacheClient;
        }

        [HttpGet("SetSlidingCacheItem")]
        public async Task<ActionResult<string>> SetSlidingCacheItem(CancellationToken cancellationToken)
        {
            var options = new DistributedCacheEntryOptions();
            options.SlidingExpiration = TimeSpan.FromSeconds(20);

            await _distributedCacheClient.SetAsync(
                "SlidingCacheItem",
                Encoding.UTF8.GetBytes(DateTime.Now.ToString()),
                options,
                cancellationToken);

            return new EmptyResult();
        }

        [HttpGet("GetSlidingCacheItem")]
        public async Task<ActionResult<string>> GetSlidingCacheItem(CancellationToken cancellationToken)
        {
            var bytes = await _distributedCacheClient.GetAsync("SlidingCacheItem", cancellationToken);

            if (bytes != null)
                return Content(Encoding.UTF8.GetString(bytes));

            return new EmptyResult();
        }

        [HttpGet("SetAbsoluteExpirationCacheItem")]
        public async Task<ActionResult<string>> SetAbsoluteExpirationCacheItem(CancellationToken cancellationToken)
        {
            var options = new DistributedCacheEntryOptions();
            options.AbsoluteExpiration = DateTime.Now.AddSeconds(20);

            await _distributedCacheClient.SetAsync(
                "AbsoluteExpirationCacheItem",
                Encoding.UTF8.GetBytes(DateTime.Now.ToString()),
                options,
                cancellationToken);

            return new EmptyResult();
        }

        [HttpGet("GetAbsoluteExpirationCacheItem")]
        public async Task<ActionResult<string>> GetAbsoluteExpirationCacheItem(CancellationToken cancellationToken)
        {
            var bytes = await _distributedCacheClient.GetAsync("AbsoluteExpirationCacheItem", cancellationToken);

            if (bytes != null)
                return Content(Encoding.UTF8.GetString(bytes));

            return new EmptyResult();
        }

        [HttpGet("{key}")]
        public async Task<ActionResult<string>> Get(string key, CancellationToken cancellationToken)
        {
            var bytes = await _distributedCacheClient.GetAsync(key, cancellationToken);

            if(bytes != null)
                return Content(Encoding.UTF8.GetString(bytes));

            return new NotFoundResult();
        }

        [HttpPut("{key}")]
        public async Task Put(string key, CancellationToken cancellationToken)
        {
            var request = HttpContext.Request;
            using (var reader = new StreamReader(request.Body))
            {
                var content = await reader.ReadToEndAsync();
                
                var options = new DistributedCacheEntryOptions();
                options.SlidingExpiration = TimeSpan.FromDays(1);
                await _distributedCacheClient.SetAsync(key, Encoding.UTF8.GetBytes(content), options, cancellationToken);
            }
        }

        [HttpPost("{key}")]
        public async Task<ActionResult<string>> Post(string key, CancellationToken cancellationToken)
        {
            var request = HttpContext.Request;
            using (var reader = new StreamReader(request.Body))
            {
                var content = await reader.ReadToEndAsync();

                var options = new DistributedCacheEntryOptions();
                options.SlidingExpiration = TimeSpan.FromDays(1);
                try
                {
                    var result = await _distributedCacheClient.CreateCachedItemAsync(
                        key,
                        Encoding.UTF8.GetBytes(content),
                        options,
                        cancellationToken);

                    if (result == null)
                    {
                        return new ConflictResult();
                    }

                    return Created("Created", Encoding.UTF8.GetString(result));
                }
                catch (Exception ex)
                {
                    return new StatusCodeResult(500);
                }
            }
        }

        [HttpDelete("{key}")]
        public async Task Delete(string key, CancellationToken cancellationToken)
        {
            await _distributedCacheClient.RemoveAsync(key, cancellationToken);
        }
    }
}
