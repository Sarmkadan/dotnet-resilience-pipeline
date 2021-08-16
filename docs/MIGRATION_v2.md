# Migration Guide: v1.x to v2.0

This document covers all breaking changes and required steps to upgrade from DotNet Resilience Pipeline v1.x to v2.0.

## Overview

v2.0 introduces Docker containerization improvements, port standardization, and infrastructure updates. The core library API remains backward-compatible - most breaking changes are in deployment and configuration.

## Breaking Changes

### 1. Default Port Changed: 5000 -> 8080

The application now listens on port **8080** by default, aligning with container runtime conventions and avoiding conflicts with macOS AirPlay Receiver.

**Before (v1.x):**
```yaml
environment:
  - ASPNETCORE_URLS=http://+:5000
ports:
  - "5000:5000"
```

**After (v2.0):**
```yaml
environment:
  - ASPNETCORE_URLS=http://+:8080
ports:
  - "8080:8080"
```

**Action required:** Update any reverse proxy configs, load balancer rules, or client URLs that reference port 5000.

### 2. Docker Base Image Changed

The runtime base image changed from `dotnet/runtime` to `dotnet/aspnet` to support HTTP health checks and the Kestrel web server.

**Before (v1.x):**
```dockerfile
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
```

**After (v2.0):**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
```

### 3. Container Runs as Non-Root

The container now runs as a non-root user (`appuser`, UID 1001). If your deployment mounts volumes that require root access, update the volume permissions accordingly.

```bash
# Fix volume permissions if needed
chown -R 1001:1001 /path/to/mounted/volume
```

### 4. Health Check Endpoint

The Docker HEALTHCHECK now uses an HTTP endpoint instead of `dotnet --version`:

**Before (v1.x):**
```dockerfile
HEALTHCHECK CMD dotnet --version || exit 1
```

**After (v2.0):**
```dockerfile
HEALTHCHECK CMD curl -f http://localhost:8080/health || exit 1
```

Ensure your application exposes a `/health` endpoint. The library provides `HealthCheckWorker` for this purpose (available since v0.6.0).

### 5. pgAdmin Removed from Default Compose Stack

The `pgadmin` service was removed from the default `docker-compose.yml` to reduce the footprint. If you need pgAdmin, add it manually or use a standalone instance.

### 6. Environment Variable for Passwords

Database and Grafana passwords now use environment variable substitution with defaults:

```yaml
POSTGRES_PASSWORD: ${POSTGRES_PASSWORD:-resilience123!}
GF_SECURITY_ADMIN_PASSWORD: ${GRAFANA_PASSWORD:-admin}
```

Set these variables in your `.env` file or CI/CD pipeline for production deployments.

## Step-by-Step Migration

### 1. Update Package Reference

```xml
<PackageReference Include="Zaiets.dotnet.resilience.pipeline" Version="2.0.0" />
```

### 2. Update Docker Files

Replace your existing `Dockerfile` and `docker-compose.yml` with the v2.0 versions from the repository.

### 3. Update Port References

Search your codebase and infrastructure configs for references to port 5000:

```bash
grep -r "5000" --include="*.yml" --include="*.yaml" --include="*.json" --include="*.cs"
```

Replace with 8080 where applicable.

### 4. Update Kubernetes Manifests

If using the provided `kubernetes-deployment.yaml`, update the container port and service port:

```yaml
containers:
  - name: resilience-pipeline
    ports:
      - containerPort: 8080
---
spec:
  ports:
    - port: 80
      targetPort: 8080
```

### 5. Verify Health Endpoint

Ensure your application registers the health check endpoint:

```csharp
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
```

### 6. Test the Migration

```bash
# Build and run with Docker Compose
docker-compose build
docker-compose up -d

# Verify health
curl http://localhost:8080/health

# Check logs
docker-compose logs -f app
```

## API Compatibility

The core library API (circuit breaker, retry, timeout, bulkhead, fallback) is fully backward-compatible. No code changes are required for policy configuration or execution. The `ResiliencyPipelineBuilder` fluent API, `DependencyInjectionExtensions`, and all policy types retain their existing signatures.

## Support

For migration issues:
- GitHub Issues: https://github.com/sarmkadan/dotnet-resilience-pipeline/issues
- Documentation: see the `docs/` directory
