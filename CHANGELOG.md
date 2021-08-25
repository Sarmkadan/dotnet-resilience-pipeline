# Changelog

All notable changes to the DotNet Resilience Pipeline project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.0.2] - 2026-05-17

### Fixed
- Fix circuit breaker half-open state allowing more requests than configured
- Added regression test for the fix

## [2.0.0] - 2026-05-16

### Added
- Add chaos engineering toolkit with fault injection scenarios
- Docker support with multi-stage builds
- Health check endpoints (/health, /health/ready)
- Integration test suite with xUnit
- Migration guide from v1.x

### Changed
- Upgraded to .NET 10.0
- Modern C# features (records, primary constructors)
- Improved API consistency

### Fixed
- Various edge cases found through testing

## [1.0.0] - 2025-09-15

### Added
- Distributed circuit breaker diagnostics and detailed state information
- Event-based observability with PolicyEvent publisher
- Comprehensive health check support for Kubernetes readiness/liveness probes
- Rate limiting middleware integration
- Performance monitoring and metrics dashboard example
- Enhanced metrics aggregation with percentile calculations (p50, p95, p99)
- Support for custom repository implementations via IRepository interface
- Bulkhead queue visualization utilities
- Extended timeout diagnostics with cancellation tracking
- CircuitBreakerDiagnostics utility for state introspection

### Changed
- Improved circuit breaker state transition logic for faster recovery detection
- Optimized retry backoff calculations for reduced memory pressure
- Enhanced bulkhead slot management with accurate queue-length tracking
- Better timeout enforcement with improved CancellationToken propagation
- Refactored policy composition for cleaner fluent API surface

### Fixed
- Circuit breaker stuck in half-open state under sustained high load
- Retry loop excessive memory use with large execution history buffers
- Timeout exceptions not properly cascading through fallback policies
- Bulkhead queue deadlock in highly concurrent scenarios
- Memory leak in event subscriber lists on repeated subscription

### Security
- Added input validation for all policy configuration parameters
- Implemented timeout protection for event subscriber callbacks
- Enhanced logging with sensitive data masking

## [0.9.0] - 2025-08-25

### Added
- Adaptive timeout policy with automatic adjustment based on observed latency
- AdaptiveTimeoutExtensions for DI registration
- ThrottlingHelper utility for rate-throttle calculations
- PolicyCacheService for fast in-process policy lookup
- Webhook manager for outbound failure notifications

### Changed
- Strengthened thread safety in CircuitBreakerService with CAS operations
- Unified exception hierarchy under ResiliencyExceptions base type
- Improved PolicyResult metadata — now includes RetryCount and CircuitBreakerState

### Fixed
- Race condition in BulkheadService queue counter on thread contention
- Fallback not executing when timeout and circuit breaker both triggered
- PolicyNameGenerator producing duplicate names under concurrent registration

## [0.8.0] - 2025-08-04

### Added
- CLI command handler and parser for running resilience checks from the terminal
- CommandOptions and CommandValidator for structured CLI input
- MetricsFormatter for human-readable console output
- CsvReportFormatter for exporting execution history to CSV
- JsonPolicySerializer for persisting and loading policy configurations

### Changed
- PerformanceMonitor refactored to report sliding-window averages
- MetricsAggregator now tracks per-policy breakdowns in addition to pipeline totals

### Fixed
- CSV formatter escaping quotes incorrectly in exception messages
- MetricsAggregator thread contention on high-frequency updates

## [0.7.0] - 2025-07-14

### Added
- Six runnable examples covering all pattern combinations
  - BasicUsage.cs — simple circuit breaker and retry
  - MicroserviceIntegration.cs — realistic upstream-service scenario
  - CircuitBreakerSimulation.cs — state machine walkthrough
  - BulkheadPatternExample.cs — resource isolation under load
  - FallbackPatternExample.cs — graceful degradation flow
  - MetricsMonitoringExample.cs — live performance tracking
- Comprehensive API reference documentation (`docs/api-reference.md`)
- Architecture overview with ASCII component diagram (`docs/architecture.md`)
- Getting started guide with step-by-step walkthrough (`docs/getting-started.md`)
- Deployment guide covering Docker, Docker Compose, and Kubernetes (`docs/deployment.md`)
- FAQ with common troubleshooting scenarios (`docs/faq.md`)
- QUICK_REFERENCE.md for at-a-glance API lookup

### Changed
- README expanded with full policy documentation, benchmarks, and troubleshooting guide

## [0.6.0] - 2025-06-23

### Added
- Docker multi-stage build with minimal runtime image
- Docker Compose configuration with Prometheus monitoring stack
- Kubernetes deployment manifest (`docs/kubernetes-deployment.yaml`)
- Prometheus scrape configuration (`prometheus.yml`)
- ResiliencyLoggingMiddleware for structured request/response logging
- ErrorHandlingMiddleware for consistent API error responses
- ExternalApiClient wrapper with built-in resilience policies
- HttpClientFactory with pre-configured resilience defaults
- HealthCheckWorker background service for continuous health polling
- MetricsCollectorWorker background service for periodic metric snapshots

### Changed
- Program.cs wired up with full middleware pipeline and background workers
- DependencyInjectionExtensions extended to cover all new services

## [0.5.0] - 2025-06-02

### Added
- ResiliencyEventPublisher with subscribe/unsubscribe support
- PipelineEventObserver interface for custom observability hooks
- MetricsAggregator with success rate, duration histograms, and execution counts
- PerformanceMonitor with real-time throughput tracking
- CircuitBreakerDiagnostics for detailed state and transition logging
- ResiliencyHelper.GenerateHealthReport() for structured health summaries
- PolicyControllers REST API (`src/Api/Controllers/`)
- MetricsController REST API endpoint for Prometheus-compatible scraping
- ExecutionHistoryRepository with query and filter support

### Changed
- ResiliencyPipelineService now publishes events on all state transitions
- PipelineStatistics extended with percentile duration fields

## [0.4.0] - 2025-05-12

### Added
- ResiliencyPipelineBuilder fluent API for chaining policy configuration
- DependencyInjectionExtensions — `AddResiliencePipeline()` for IServiceCollection
- PolicyRepository with CRUD operations and name-based lookup
- IRepository generic interface for custom storage backends
- PolicyValidationHelper with compile-time and runtime checks
- PolicyNameGenerator for deterministic policy naming
- ResiliencyConstants for shared configuration defaults

### Changed
- All services now registered via DI rather than direct instantiation
- PolicyResult<T> generic wrapper added with Value, Error, Duration, and RetryCount properties

### Fixed
- Service constructor parameter ordering causing DI resolution failures
- Missing null checks in builder chain causing NullReferenceException on misconfigured pipelines

## [0.3.0] - 2025-04-21

### Added
- BulkheadPolicy with configurable MaxParallelization and MaxQueueLength
- BulkheadService with SemaphoreSlim-based slot management
- BulkheadRejectedException when queue capacity is exceeded
- FallbackPolicy with FallbackOnAnyException and FallbackTimeout options
- FallbackService executing secondary delegate on primary failure
- AdaptiveTimeoutPolicy initial scaffolding

### Changed
- CircuitBreakerService refactored to share synchronization primitives with BulkheadService
- RetryService now cancels pending retry attempts when CancellationToken fires

### Fixed
- BulkheadService not releasing semaphore on operation exception
- Fallback not invoked when operation throws synchronously

## [0.2.0] - 2025-04-02

### Added
- RetryPolicy supporting Fixed, Linear, and Exponential backoff strategies
- RetryService with configurable MaxRetries, InitialDelay, BackoffMultiplier, and MaxDelay
- Jitter applied to exponential backoff to prevent thundering herd
- TimeoutPolicy enforcing maximum execution duration via CancellationToken
- TimeoutService with OperationTimeoutException on breach
- ResiliencyPipelineService as top-level orchestrator composing all policies
- PolicyResult non-generic and generic variants
- Basic execution history storage

### Changed
- CircuitBreakerPolicy now exposes CurrentState for external inspection
- Project restructured into Domain, Services, Data, and Configuration layers

### Fixed
- CircuitBreakerService not resetting failure counter after successful half-open probe
- RetryService not respecting CancellationToken between retry attempts

## [0.1.0] - 2025-03-14

### Added
- CircuitBreakerPolicy with three-state machine (Closed → Open → Half-Open)
- CircuitBreakerService with configurable FailureThreshold, OpenDuration, and SuccessThresholdInHalfOpen
- CircuitBreakerOpenException thrown when circuit is open
- ResiliencyPolicy base class and domain model
- Initial project structure: DotNetResiliencePipeline.csproj targeting .NET 10.0
- Solution file with test project reference
- MIT License
- Initial .gitignore for .NET projects

---

## Release Notes

### v2.0.0
Docker and deployment overhaul. Port standardized to 8080, non-root container, HTTP health checks. Core library API remains backward-compatible. See `docs/MIGRATION_v2.md` for upgrade steps.

### v1.0.0
Stable release. Adds distributed-circuit-breaker diagnostics, Kubernetes health-check integration, and comprehensive observability. No breaking changes from v0.9.0.

### v0.9.0
Adaptive timeout, policy caching, and webhook notifications. Hardened thread safety across all services.

### v0.8.0
CLI tooling and reporting formatters — run resilience checks from the terminal and export metrics to CSV/JSON.

### v0.7.0
Full documentation suite and six runnable examples. Ready for early adopters.

### v0.6.0
Docker, Docker Compose, and Kubernetes support. Middleware pipeline and background workers wired up.

### v0.5.0
Observability layer: event publishing, metrics aggregation, performance monitoring, and REST API endpoints.

### v0.4.0
Fluent builder API and dependency injection integration. Policies now fully composable via `AddResiliencePipeline()`.

### v0.3.0
Bulkhead and fallback patterns added. All five core resilience patterns now implemented.

### v0.2.0
Retry and timeout policies added. Pipeline orchestrator introduced.

### v0.1.0
Initial release with circuit breaker implementation and core project scaffolding.

---

## Upgrade Guide

### From 1.0.0 to 2.0.0
Port changed from 5000 to 8080. Docker base image changed to `aspnet`. Container runs as non-root. pgAdmin removed from default compose. See `docs/MIGRATION_v2.md` for full details.

### From 0.9.0 to 1.0.0
No breaking changes. New observability and health-check features are additive and opt-in.

### From 0.8.0 to 0.9.0
No breaking changes. Adaptive timeout is a new policy type; existing policies are unaffected.

### From 0.7.0 to 0.8.0
No breaking changes. CLI and formatter types are new additions.

### Earlier versions
Each release from 0.1.0 to 0.7.0 is additive. Existing policy configuration code requires no changes across minor version bumps.

---

## Support

For issues, feature requests, or questions:
- GitHub: https://github.com/sarmkadan/dotnet-resilience-pipeline
- Documentation: see the `docs/` directory

## License

All changes are covered under the MIT License.
See LICENSE file for details.
