# Changelog

All notable changes to the DotNet Resilience Pipeline project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.2.0] - 2026-01-15

### Added
- Distributed circuit breaker support for multi-instance deployments
- Circuit breaker diagnostics and detailed state information
- Event-based observability with PolicyEvent publisher
- Comprehensive health check support for Kubernetes
- Rate limiting middleware integration
- Performance monitoring dashboard example
- Enhanced metrics aggregation with percentile calculations
- Support for custom repository implementations
- Bulkhead queue visualization utilities
- Extended timeout diagnostics with cancellation tracking

### Changed
- Improved circuit breaker state transition logic for faster recovery detection
- Optimized retry backoff calculations for better cache efficiency
- Enhanced bulkhead slot management with dynamic adjustment
- Better timeout enforcement with improved CancellationToken handling
- Refactored policy composition for cleaner fluent API

### Fixed
- Circuit breaker stuck in half-open state under high load
- Retry loop consuming excessive memory with large history buffers
- Timeout exceptions not properly cascading through fallback policies
- Bulkhead queue deadlock in concurrent scenarios
- Memory leak in event subscriber lists

### Security
- Added input validation for all policy configurations
- Implemented timeout protection for event subscribers
- Enhanced logging security with sensitive data masking

## [1.1.0] - 2025-10-20

### Added
- Exponential backoff with jitter for retry policies
- Linear backoff strategy option for gradual delays
- Configurable retry exceptions filtering
- Fallback execution timeout support
- Execution history repository with query support
- Metrics aggregation with success rate calculations
- Health report generation utility
- Policy validation helper methods
- CSV report formatter for metrics export
- JSON policy serialization support
- Docker support with multi-stage builds
- Docker Compose configuration with monitoring stack
- Comprehensive API reference documentation
- Architecture documentation with diagrams
- Getting started guide with code examples
- Deployment guide for production environments
- FAQ document with troubleshooting

### Changed
- Improved bulkhead performance with optimized semaphore usage
- Enhanced circuit breaker state machine with better documentation
- Streamlined DependencyInjection extension methods
- Simplified PolicyResult generic wrapper interface
- Better exception messages for all resilience patterns

### Fixed
- Circuit breaker not transitioning from half-open to closed state
- Retry delay calculation overflow on large backoff multipliers
- Bulkhead queue length not properly tracked under concurrent load
- Timeout not enforced on fallback operations
- Event subscriber memory leaks on repeated subscription

### Documentation
- Added comprehensive README with 2000+ words
- Created 4 detailed documentation files
- Added 6 runnable examples
- Created API reference documentation

## [1.0.0] - 2025-07-10

### Added
- Circuit Breaker pattern implementation with three-state machine
  - Closed: normal operation
  - Open: reject requests
  - Half-Open: test recovery
- Configurable failure threshold and open duration
- Automatic recovery detection in half-open state

- Retry policy with multiple backoff strategies
  - Fixed delay between retries
  - Linear increasing delays
  - Exponential backoff (recommended)
- Configurable max retries and initial delay
- Support for retryable exception type filtering
- Max delay capping to prevent excessive waits

- Timeout enforcement using CancellationToken
- Graceful timeout handling with low overhead
- Integration with other policies

- Bulkhead pattern for resource isolation
- Configurable max parallelization limits
- Queue management for rejected requests
- Semaphore-based slot acquisition/release

- Fallback pattern for graceful degradation
- Exception type-based fallback triggering
- Timeout-aware fallback execution
- Fallback result chaining

- Fluent builder pattern for intuitive configuration
  - Chainable method calls
  - Inline policy configuration
  - Type-safe policy definitions

- Dependency Injection integration
  - Microsoft.Extensions.DependencyInjection support
  - Automatic service registration
  - Policy repository injection

- Comprehensive metrics and statistics
  - Total execution count
  - Success/failure rates
  - Duration tracking (min, max, average)
  - Circuit breaker state monitoring

- Thread-safe implementation
  - Lock-based synchronization for shared state
  - Atomic operations for counters
  - Concurrent execution support
  - No race conditions

- Generic PolicyResult<T> wrapper
  - Success/failure indication
  - Value and exception properties
  - Metadata (duration, retry count, circuit state)
  - Strongly-typed results

- Policy persistence
  - In-memory policy repository
  - Policy CRUD operations
  - Policy lookup by name

- Execution history tracking
  - Failed and successful execution records
  - Execution duration tracking
  - Exception logging
  - History queries and filtering

- Utilities and helpers
  - Policy validation helpers
  - Policy name generation
  - Performance monitoring
  - Throttling helpers
  - Diagnostic tools

- Middleware for Web APIs
  - Error handling middleware
  - Resilience logging middleware
  - Rate limiting middleware

- Integration utilities
  - HTTP client factory
  - External API client wrapper
  - Webhook manager for notifications

- Event-driven architecture
  - Event publisher for policy events
  - Pipeline event observer interface
  - Extensible event system

### Technical Details
- Target: .NET 10.0
- Language: C# 13 with latest features
- Dependencies: Microsoft.Extensions.* only
- Thread-safe with proper synchronization
- Zero external dependencies for core library
- Minimal memory footprint
- <1ms overhead per policy check

### Project Structure
- Domain layer: Policy implementations and contracts
- Service layer: Policy execution orchestration
- Data layer: Repository pattern for persistence
- Configuration layer: Builder pattern setup
- Utilities: Helpers and monitoring tools
- Middleware: HTTP request/response handling
- Integration: External service adapters
- Events: Pub/sub event system

### Documentation
- Inline XML documentation on all public APIs
- Method-level comments explaining logic
- README with feature overview
- License file (MIT)
- .gitignore for .NET projects

---

## Release Notes

### v1.2.0
Focus on observability and production-readiness. Added distributed circuit breaker support, enhanced health checks, and comprehensive monitoring capabilities.

### v1.1.0
Major documentation and deployment updates. Added complete documentation suite, Docker support, and six runnable examples demonstrating all patterns.

### v1.0.0
Initial release with all core resilience patterns: circuit breaker, retry, timeout, bulkhead, and fallback with fluent configuration and full thread-safety.

---

## Upgrade Guide

### From 1.1.0 to 1.2.0
No breaking changes. New features are additive and backward compatible.

- New distributed circuit breaker features are optional
- Existing code continues to work without changes
- New metrics and observability features available for opt-in

### From 1.0.0 to 1.1.0
No breaking changes. All new features are additions.

- Existing policies work unchanged
- New backoff strategies available but optional
- Event system is optional and backward compatible

---

## Future Roadmap

### Planned for v1.3.0
- Async context management improvements
- Custom metric exporters
- OpenTelemetry integration
- Advanced circuit breaker patterns (sliding window)
- Policy composition validators

### Planned for v2.0.0
- Distributed tracing support
- Policy versioning and migrations
- Advanced scheduling for retry/backoff
- Reactive policy triggers
- Performance optimizations for ultra-high throughput

---

## Support

For issues, feature requests, or questions, please visit:
- GitHub: https://github.com/sarmkadan/dotnet-resilience-pipeline
- Documentation: See docs/ directory

## License

All changes are covered under the MIT License.
See LICENSE file for details.
