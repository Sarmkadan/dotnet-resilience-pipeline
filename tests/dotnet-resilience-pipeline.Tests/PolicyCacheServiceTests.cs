using System;
using DotNetResiliencePipeline.Caching;
using Xunit;

namespace DotNetResiliencePipeline.Tests;

public class PolicyCacheServiceTests
{
    private readonly PolicyCacheService _service = new();

    [Fact]
    public void DefaultValues_AreSetCorrectly()
    {
        Assert.Equal(TimeSpan.FromMinutes(5), _service.DefaultTtl);
        Assert.Equal(1000, _service.MaxCacheSize);
    }

    [Fact]
    public void Set_And_Get_WorkAsExpected()
    {
        var policyName = "policy-1";
        var config = new { Foo = "Bar" };

        _service.Set(policyName, config);
        var cached = _service.Get(policyName);

        Assert.NotNull(cached);
        Assert.Equal(policyName, cached!.PolicyName);
        Assert.Same(config, cached.Config);
        Assert.Equal(1, cached.AccessCount);
    }

    [Fact]
    public void Get_IncrementsAccessCount_And_UpdatesLastAccessTime()
    {
        var policyName = "policy-2";
        var config = new { Value = 42 };
        _service.Set(policyName, config);

        var first = _service.Get(policyName);
        var before = first!.LastAccessTime;

        var second = _service.Get(policyName);
        Assert.Equal(2, second!.AccessCount);
        Assert.True(second.LastAccessTime > before);
    }

    [Fact]
    public void GetOrLoad_LoadsWhenMissing_And_CachesResult()
    {
        var policyName = "policy-3";
        var config = new { Data = "X" };
        CachedPolicy? loader(string name) => new CachedPolicy
        {
            PolicyName = name,
            Config = config,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(1),
            AccessCount = 0,
            LastAccessTime = DateTime.UtcNow
        };

        var loaded = _service.GetOrLoad(policyName, loader);
        Assert.NotNull(loaded);
        Assert.Equal(policyName, loaded!.PolicyName);
        Assert.Same(config, loaded.Config);

        // Subsequent call should return the cached instance, not invoke loader again
        var cached = _service.Get(policyName);
        Assert.Same(loaded, cached);
    }

    [Fact]
    public void Invalidate_RemovesEntry_ReturnsTrue()
    {
        var policyName = "policy-4";
        _service.Set(policyName, new { });
        Assert.NotNull(_service.Get(policyName));

        var removed = _service.Invalidate(policyName);
        Assert.True(removed);
        Assert.Null(_service.Get(policyName));
    }

    [Fact]
    public void Clear_EvictsAllEntries()
    {
        _service.Set("a", new { });
        _service.Set("b", new { });
        Assert.NotNull(_service.Get("a"));
        Assert.NotNull(_service.Get("b"));

        _service.Clear();
        Assert.Null(_service.Get("a"));
        Assert.Null(_service.Get("b"));
    }

    [Fact]
    public void GetStatistics_ReflectsCurrentState()
    {
        _service.Clear();
        _service.Set("valid", new { });
        _service.Set("expired", new { });

        // Force expiration of one entry
        var expired = _service.Get("expired")!;
        typeof(CachedPolicy).GetProperty(nameof(CachedPolicy.ExpiresAt))!
            .SetValue(expired, DateTime.UtcNow.AddSeconds(-1));

        var stats = _service.GetStatistics();

        Assert.Equal(2, stats.TotalEntries);
        Assert.Equal(1, stats.ValidEntries);
        Assert.Equal(1, stats.ExpiredEntries);
        Assert.Equal(100.0, stats.HitRate); // only one valid entry with AccessCount = 1
        Assert.True(stats.AverageTtl > TimeSpan.Zero);
    }

    [Fact]
    public void CleanupExpired_RemovesOnlyExpiredEntries()
    {
        _service.Clear();
        _service.Set("good", new { });
        _service.Set("old", new { });

        var old = _service.Get("old")!;
        typeof(CachedPolicy).GetProperty(nameof(CachedPolicy.ExpiresAt))!
            .SetValue(old, DateTime.UtcNow.AddSeconds(-1));

        var removedCount = _service.CleanupExpired();
        Assert.Equal(1, removedCount);
        Assert.Null(_service.Get("old"));
        Assert.NotNull(_service.Get("good"));
    }

    [Fact]
    public void Get_ThrowsArgumentNullException_OnNullOrWhiteSpace()
    {
        Assert.Throws<ArgumentNullException>(() => _service.Get(null!));
        Assert.Throws<ArgumentNullException>(() => _service.Get(string.Empty));
        Assert.Throws<ArgumentNullException>(() => _service.Get("   "));
    }

    [Fact]
    public void Set_ThrowsArgumentNullException_OnInvalidArguments()
    {
        Assert.Throws<ArgumentNullException>(() => _service.Set(null!, new { }));
        Assert.Throws<ArgumentNullException>(() => _service.Set(string.Empty, new { }));
        Assert.Throws<ArgumentNullException>(() => _service.Set("name", null!));
    }

    [Fact]
    public void GetOrLoad_ThrowsArgumentNullException_OnInvalidArguments()
    {
        Assert.Throws<ArgumentNullException>(() => _service.GetOrLoad(null!, _ => null!));
        Assert.Throws<ArgumentNullException>(() => _service.GetOrLoad(string.Empty, _ => null!));
        Assert.Throws<ArgumentNullException>(() => _service.GetOrLoad("name", null!));
    }
}
