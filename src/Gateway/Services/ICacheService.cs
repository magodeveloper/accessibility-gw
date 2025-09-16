using Gateway.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Gateway.Services;

public interface ICacheService
{
    string GenerateCacheKey(TranslateRequest request);
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task InvalidateServiceCacheAsync(string service, CancellationToken cancellationToken = default);
}