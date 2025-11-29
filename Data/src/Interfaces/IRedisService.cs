namespace NodPT.Data.Interfaces
{
    /// <summary>
    /// Unified interface for all Redis operations, combining Queue and Cache functionality.
    /// 
    /// This interface extends both <see cref="IRedisQueueService"/> and <see cref="IRedisCacheService"/>
    /// for backward compatibility. New code should prefer using the specific interfaces:
    /// 
    /// <list type="bullet">
    /// <item>
    /// <term><see cref="IRedisQueueService"/></term>
    /// <description>For message queuing (Add, Listen, Acknowledge, etc.)</description>
    /// </item>
    /// <item>
    /// <term><see cref="IRedisCacheService"/></term>
    /// <description>For caching (Get, Set, Update, Range, etc.)</description>
    /// </item>
    /// </list>
    /// </summary>
    /// <example>
    /// <code>
    /// // Prefer specific interfaces for new code:
    /// public class MyQueueWorker
    /// {
    ///     private readonly IRedisQueueService _queue;
    ///     public MyQueueWorker(IRedisQueueService queue) => _queue = queue;
    /// }
    /// 
    /// public class MyCacheService  
    /// {
    ///     private readonly IRedisCacheService _cache;
    ///     public MyCacheService(IRedisCacheService cache) => _cache = cache;
    /// }
    /// 
    /// // IRedisService still works for backward compatibility:
    /// public class LegacyService
    /// {
    ///     private readonly IRedisService _redis;
    ///     public LegacyService(IRedisService redis) => _redis = redis;
    /// }
    /// </code>
    /// </example>
    public interface IRedisService : IRedisQueueService, IRedisCacheService
    {
        // This interface combines IRedisQueueService and IRedisCacheService
        // for backward compatibility. All methods are inherited from the base interfaces.
    }
}
