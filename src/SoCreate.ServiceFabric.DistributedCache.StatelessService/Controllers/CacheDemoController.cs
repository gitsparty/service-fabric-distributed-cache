using System;
using System.Fabric;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using SoCreate.ServiceFabric.DistributedCache.StatefulService.Client;

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
        public async Task<ActionResult<string>> SetSlidingCacheItem()
        {
            var options = new DistributedCacheEntryOptions();
            options.SlidingExpiration = TimeSpan.FromSeconds(20);

            await _distributedCacheClient.SetAsync("SlidingCacheItem", Encoding.UTF8.GetBytes(DateTime.Now.ToString()), options);

            return new EmptyResult();
        }

        [HttpGet("GetSlidingCacheItem")]
        public async Task<ActionResult<string>> GetSlidingCacheItem()
        {
            var bytes = await _distributedCacheClient.GetAsync("SlidingCacheItem");

            if (bytes != null)
                return Content(Encoding.UTF8.GetString(bytes));

            return new EmptyResult();
        }

        [HttpGet("SetAbsoluteExpirationCacheItem")]
        public async Task<ActionResult<string>> SetAbsoluteExpirationCacheItem()
        {
            var options = new DistributedCacheEntryOptions();
            options.AbsoluteExpiration = DateTime.Now.AddSeconds(20);

            await _distributedCacheClient.SetAsync("AbsoluteExpirationCacheItem", Encoding.UTF8.GetBytes(DateTime.Now.ToString()), options);

            return new EmptyResult();
        }

        [HttpGet("GetAbsoluteExpirationCacheItem")]
        public async Task<ActionResult<string>> GetAbsoluteExpirationCacheItem()
        {
            var bytes = await _distributedCacheClient.GetAsync("AbsoluteExpirationCacheItem");

            if (bytes != null)
                return Content(Encoding.UTF8.GetString(bytes));

            return new EmptyResult();
        }

        [HttpGet("{key}")]
        public async Task<ActionResult<string>> Get(string key)
        {
            var bytes = await _distributedCacheClient.GetAsync(key);

            if(bytes != null)
                return Content(Encoding.UTF8.GetString(bytes));

            return new NotFoundResult();
        }

        [HttpPut("{key}")]
        public async Task Put(string key)
        {
            var request = HttpContext.Request;
            using (var reader = new StreamReader(request.Body))
            {
                var content = await reader.ReadToEndAsync();
                
                var options = new DistributedCacheEntryOptions();
                options.SlidingExpiration = TimeSpan.FromDays(1);
                await _distributedCacheClient.SetAsync(key, Encoding.UTF8.GetBytes(content), options);
            }
        }

        [HttpPost("{key}")]
        public async Task<ActionResult<string>> Post(string key)
        {
            var request = HttpContext.Request;
            using (var reader = new StreamReader(request.Body))
            {
                var content = await reader.ReadToEndAsync();

                var options = new DistributedCacheEntryOptions();
                options.SlidingExpiration = TimeSpan.FromDays(1);
                try
                {
                    var result = await _distributedCacheClient.CreateCachedItemAsync(key, Encoding.UTF8.GetBytes(content), options);

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
        public async Task Delete(string key)
        {
            await _distributedCacheClient.RemoveAsync(key);
        }
    }
}
