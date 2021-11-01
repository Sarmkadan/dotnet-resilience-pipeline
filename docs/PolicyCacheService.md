# PolicyCacheService

A thread-safe in-memory cache for storing and retrieving `CachedPolicy` instances with configurable time-to-live (TTL) and automatic expiration. The service tracks cache statistics and provides methods for managing cached policies, including invalidation and cleanup of expired entries.

## API

### `public TimeSpan DefaultTtl`
Gets the default time-to-live duration applied to policies when no explicit TTL is provided during insertion. This value is used by the `Set` method if no TTL is specified.

### `public int MaxCacheSize`
Gets the maximum number of entries allowed in the cache. When the cache exceeds this size, the least recently used (LRU) entries are automatically removed during subsequent operations.

### `public CachedPolicy? Get(string key)`
Retrieves the cached policy associated with the specified key.
- **Parameters**:
  - `key` (string): The unique identifier of the policy to retrieve.
- **Return value**: The `CachedPolicy` instance if found and not expired; otherwise, `null`.
- **Exceptions**: Throws `ArgumentNullException` if `key` is `null`.

### `public void Set(string key, CachedPolicy policy, TimeSpan? ttl = null)`
Stores a policy in the cache with an optional TTL. If the TTL is not provided, `DefaultTtl` is used.
- **Parameters**:
  - `key` (string): The unique identifier for the policy.
  - `policy` (`CachedPolicy`): The policy to cache.
  - `ttl` (`TimeSpan?`, optional): The time-to-live for the entry. If `null`, `DefaultTtl` is used.
- **Exceptions**: Throws `ArgumentNullException` if `key` or `policy` is `null`.

### `public bool Invalidate(string key)`
Removes the cached policy associated with the specified key if it exists and is not expired.
- **Parameters**:
  - `key` (string): The unique identifier of the policy to invalidate.
- **Return value**: `true` if the entry was found and removed; otherwise, `false`.
- **Exceptions**: Throws `ArgumentNullException` if `key` is `null`.

### `public void Clear()`
Removes all entries from the cache, including expired and valid entries.

### `public CacheStatistics GetStatistics()`
Retrieves the current statistics of the cache, including entry counts, hit rate, and TTL metrics.
- **Return value**: A `CacheStatistics` object containing cache metrics.

### `public int CleanupExpired()`
Removes all expired entries from the cache and returns the number of entries removed.
- **Return value**: The count of expired entries removed.

### `public string PolicyName`
Gets the name of the policy type this cache is configured to store (e.g., "CircuitBreaker", "Retry").

### `public object Config`
Gets the configuration object used to initialize this cache instance. The type and contents depend on the specific policy implementation.

### `public DateTime CreatedAt`
Gets the UTC timestamp when this cache instance was created.

### `public DateTime ExpiresAt`
Gets the UTC timestamp when this cache instance will expire. This is calculated as `CreatedAt + DefaultTtl` unless explicitly configured otherwise.

### `public DateTime LastAccessTime`
Gets the UTC timestamp of the last access (read or write) to this cache instance.

### `public long AccessCount`
Gets the total number of accesses (reads or writes) to this cache instance since creation.

### `public int TotalEntries`
Gets the total number of entries currently in the cache, including expired and valid entries.

### `public int ValidEntries`
Gets the number of non-expired entries currently in the cache.

### `public int ExpiredEntries`
Gets the number of entries in the cache that have expired but have not yet been removed.

### `public double HitRate`
Gets the ratio of cache hits to total accesses (reads) since the last statistics reset or cache creation. A value of `1.0` indicates all reads were hits.

### `public TimeSpan AverageTtl`
Gets the average time-to-live (TTL) of all valid entries in the cache, measured in seconds.

## Usage

### Example 1: Basic Policy Caching
