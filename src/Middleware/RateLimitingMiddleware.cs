#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using System.Threading;

namespace DotNetResiliencePipeline.Middleware;

/// <summary>
/// Middleware for rate limiting API requests and policy executions.
/// Implements token bucket algorithm with configurable limits per client/policy.
/// </summary>
[JsonSerializable(typeof(RateLimitingMiddleware))]
[JsonSerializable(typeof(RateLimiter))]
[JsonSerializable(typeof(RateLimitStatus))]
public sealed class RateLimitingMiddleware
{
	[JsonInclude]
	private readonly ConcurrentDictionary<string, RateLimiter> _limiters = new();

	[JsonInclude]
	private readonly object _limitersLock = new object();

	[JsonInclude]
	private int _defaultRequestsPerSecond = 100;

	[JsonInclude]
	private int _defaultRequestsPerMinute = 5000;

	[JsonConstructor]
	public RateLimitingMiddleware()
	{
	}

	/// <summary>
	/// Configures default rate limits for the middleware.
	/// </summary>
	public void ConfigureLimits(int requestsPerSecond, int requestsPerMinute)
	{
		_defaultRequestsPerSecond = requestsPerSecond;
		_defaultRequestsPerMinute = requestsPerMinute;
	}

	/// <summary>
	/// Checks if a request is allowed under rate limit for a client.
	/// </summary>
	public bool IsRequestAllowed(string clientId, int tokensRequired = 1)
	{
		lock (_limitersLock)
		{
			var limiter = _limiters.GetOrAdd(clientId,
				new RateLimiter(_defaultRequestsPerSecond, _defaultRequestsPerMinute));

			return limiter.TryConsumeTokens(tokensRequired);
		}
	}

	/// <summary>
	/// Gets the current rate limit status for a client.
	/// </summary>
	public RateLimitStatus GetStatus(string clientId)
	{
		lock (_limitersLock)
		{
			if (_limiters.TryGetValue(clientId, out var limiter))
				return limiter.GetStatus();

			// Return default limits for unknown client
			return new RateLimitStatus
			{
				ClientId = clientId,
				RequestsPerSecond = _defaultRequestsPerSecond,
				RequestsPerMinute = _defaultRequestsPerMinute,
				RemainingTokensPerSecond = _defaultRequestsPerSecond,
				RemainingTokensPerMinute = _defaultRequestsPerMinute,
				NextResetSecond = DateTime.UtcNow.AddSeconds(1),
				NextResetMinute = DateTime.UtcNow.AddMinutes(1)
			};
		}
	}

	/// <summary>
	/// Gets rate limit status for all clients.
	/// </summary>
	public Dictionary<string, RateLimitStatus> GetAllStatus()
	{
		lock (_limitersLock)
		{
			return _limiters.ToDictionary(
				x => x.Key,
				x => x.Value.GetStatus());
		}
	}

	/// <summary>
	/// Resets rate limit for a specific client.
	/// </summary>
	public void ResetClient(string clientId)
	{
		lock (_limitersLock)
		{
			_limiters.TryRemove(clientId, out _);
		}
	}

	/// <summary>
	/// Clears all rate limiters.
	/// </summary>
	public void ClearAll()
	{
		lock (_limitersLock)
		{
			_limiters.Clear();
		}
	}

	/// <summary>
	/// Returns a concise, informative representation of the middleware state.
	/// </summary>
	public override string ToString() =>
		$"RateLimitingMiddleware {{ DefaultRequestsPerSecond = {_defaultRequestsPerSecond}, DefaultRequestsPerMinute = {_defaultRequestsPerMinute}, ActiveClients = {_limiters.Count} }}";
}

/// <summary>
/// Individual rate limiter using token bucket algorithm.
/// </summary>
[JsonSerializable(typeof(RateLimiter))]
public sealed class RateLimiter
{
	[JsonInclude]
	private readonly int _tokensPerSecond;

	[JsonInclude]
	private readonly int _tokensPerMinute;

	[JsonInclude]
	private long _tokensSecond;

	[JsonInclude]
	private long _tokensMinute;

	[JsonInclude]
	private DateTime _lastRefillSecond;

	[JsonInclude]
	private DateTime _lastRefillMinute;

	[JsonIgnore]
	private object _lockObj = new object();

	[JsonConstructor]
	public RateLimiter(int tokensPerSecond, int tokensPerMinute, long tokensSecond, long tokensMinute, DateTime lastRefillSecond, DateTime lastRefillMinute)
	{
		_tokensPerSecond = tokensPerSecond;
		_tokensPerMinute = tokensPerMinute;
		_tokensSecond = tokensSecond;
		_tokensMinute = tokensMinute;
		_lastRefillSecond = lastRefillSecond;
		_lastRefillMinute = lastRefillMinute;
		_lockObj = new object();
	}

	public RateLimiter(int tokensPerSecond, int tokensPerMinute)
	{
		_tokensPerSecond = tokensPerSecond;
		_tokensPerMinute = tokensPerMinute;
		_tokensSecond = tokensPerSecond;
		_tokensMinute = tokensPerMinute;
		_lastRefillSecond = DateTime.UtcNow;
		_lastRefillMinute = DateTime.UtcNow;
	}

	/// <summary>
	/// Attempts to consume tokens. Returns true if successful.
	/// </summary>
	public bool TryConsumeTokens(int tokensRequired)
	{
		lock (_lockObj)
		{
			RefillTokens();

			// Check both limits
			if (_tokensSecond >= tokensRequired && _tokensMinute >= tokensRequired)
			{
				_tokensSecond -= tokensRequired;
				_tokensMinute -= tokensRequired;
				return true;
			}

			return false;
		}
	}

	/// <summary>
	/// Gets current rate limit status.
	/// </summary>
	public RateLimitStatus GetStatus()
	{
		lock (_lockObj)
		{
			RefillTokens();

			return new RateLimitStatus
			{
				RequestsPerSecond = _tokensPerSecond,
				RequestsPerMinute = _tokensPerMinute,
				RemainingTokensPerSecond = (int)Math.Min(_tokensSecond, _tokensPerSecond),
				RemainingTokensPerMinute = (int)Math.Min(_tokensMinute, _tokensPerMinute),
				NextResetSecond = _lastRefillSecond.AddSeconds(1),
				NextResetMinute = _lastRefillMinute.AddMinutes(1)
			};
		}
	}

	private void RefillTokens()
	{
		var now = DateTime.UtcNow;

		// Refill per-second bucket
		if ((now - _lastRefillSecond).TotalSeconds >= 1)
		{
			var intervalsPassed = (int)(now - _lastRefillSecond).TotalSeconds;
			_tokensSecond = Math.Min(_tokensSecond + (_tokensPerSecond * intervalsPassed), _tokensPerSecond);
			_lastRefillSecond = now;
		}

		// Refill per-minute bucket
		if ((now - _lastRefillMinute).TotalMinutes >= 1)
		{
			var intervalsPassed = (int)(now - _lastRefillMinute).TotalMinutes;
			_tokensMinute = Math.Min(_tokensMinute + (_tokensPerMinute * intervalsPassed), _tokensPerMinute);
			_lastRefillMinute = now;
		}
	}
}

/// <summary>
/// Rate limit status for a client.
/// </summary>
[JsonSerializable(typeof(RateLimitStatus))]
public sealed class RateLimitStatus
{
	public string? ClientId { get; set; }
	public int RequestsPerSecond { get; set; }
	public int RequestsPerMinute { get; set; }
	public int RemainingTokensPerSecond { get; set; }
	public int RemainingTokensPerMinute { get; set; }
	public DateTime NextResetSecond { get; set; }
	public DateTime NextResetMinute { get; set; }
	[JsonIgnore]
	public bool IsLimited => RemainingTokensPerSecond <= 0 || RemainingTokensPerMinute <= 0;

	[JsonConstructor]
	public RateLimitStatus()
	{
	}
}
