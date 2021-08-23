# Deployment Guide

Production deployment strategies and best practices for DotNet Resilience Pipeline.

## Prerequisites

- .NET 10.0 runtime
- For Docker: Docker 20.10+
- For Kubernetes: Kubernetes 1.20+

## Local Deployment

### Development Environment

```bash
# Clone repository
git clone https://github.com/sarmkadan/dotnet-resilience-pipeline.git
cd dotnet-resilience-pipeline

# Restore dependencies
dotnet restore

# Build project
dotnet build

# Run tests (if available)
dotnet test

# Create NuGet package
dotnet pack -c Release
```

### Local Testing

```bash
# Build example application
cd examples
dotnet build

# Run example
dotnet run --project BasicUsage.csproj
```

## Docker Deployment

### Build Docker Image

```dockerfile
# Dockerfile in project root
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["DotNetResiliencePipeline.csproj", "."]
RUN dotnet restore "DotNetResiliencePipeline.csproj"
COPY . .
RUN dotnet build "DotNetResiliencePipeline.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DotNetResiliencePipeline.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DotNetResiliencePipeline.dll"]
```

### Build and Run

```bash
# Build image
docker build -t dotnet-resilience-pipeline:latest .

# Run container
docker run -d \
  --name resilience-pipeline \
  -p 5000:5000 \
  -e ASPNETCORE_URLS=http://+:5000 \
  dotnet-resilience-pipeline:latest

# View logs
docker logs -f resilience-pipeline
```

## Docker Compose

### Multi-Service Setup

```yaml
# docker-compose.yml
version: '3.8'

services:
  api:
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - "5000:5000"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:5000
      - ConnectionStrings__Default=Server=postgres;Database=resilience;
    depends_on:
      - postgres
    networks:
      - resilience-net

  postgres:
    image: postgres:16-alpine
    environment:
      POSTGRES_DB: resilience
      POSTGRES_PASSWORD: changeme
    volumes:
      - postgres_data:/var/lib/postgresql/data
    networks:
      - resilience-net

  monitoring:
    image: prom/prometheus:latest
    ports:
      - "9090:9090"
    volumes:
      - ./prometheus.yml:/etc/prometheus/prometheus.yml
    networks:
      - resilience-net

volumes:
  postgres_data:

networks:
  resilience-net:
    driver: bridge
```

### Start Services

```bash
docker-compose up -d
docker-compose logs -f api
```

## Kubernetes Deployment

### ConfigMap and Secrets

```yaml
# kubernetes/configmap.yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: resilience-config
data:
  appsettings.json: |
    {
      "CircuitBreaker": {
        "FailureThreshold": 5,
        "OpenDuration": "00:00:30"
      },
      "Retry": {
        "MaxRetries": 3,
        "InitialDelay": "00:00:00.1000000"
      }
    }
---
apiVersion: v1
kind: Secret
metadata:
  name: resilience-secrets
type: Opaque
data:
  ConnectionString: (base64 encoded)
```

### Deployment

```yaml
# kubernetes/deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: resilience-pipeline
  labels:
    app: resilience-pipeline
spec:
  replicas: 3
  selector:
    matchLabels:
      app: resilience-pipeline
  template:
    metadata:
      labels:
        app: resilience-pipeline
    spec:
      containers:
      - name: api
        image: dotnet-resilience-pipeline:latest
        ports:
        - containerPort: 5000
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: ASPNETCORE_URLS
          value: "http://+:5000"
        - name: ConnectionString
          valueFrom:
            secretKeyRef:
              name: resilience-secrets
              key: ConnectionString
        volumeMounts:
        - name: config
          mountPath: /app/appsettings.json
          subPath: appsettings.json
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "512Mi"
            cpu: "500m"
        livenessProbe:
          httpGet:
            path: /health
            port: 5000
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /health/ready
            port: 5000
          initialDelaySeconds: 5
          periodSeconds: 5
      volumes:
      - name: config
        configMap:
          name: resilience-config
```

### Service and Ingress

```yaml
# kubernetes/service.yaml
apiVersion: v1
kind: Service
metadata:
  name: resilience-pipeline-service
spec:
  type: LoadBalancer
  ports:
  - port: 80
    targetPort: 5000
  selector:
    app: resilience-pipeline
---
# kubernetes/ingress.yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: resilience-ingress
spec:
  ingressClassName: nginx
  rules:
  - host: api.example.com
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: resilience-pipeline-service
            port:
              number: 80
```

### Deploy to Kubernetes

```bash
# Create namespace
kubectl create namespace resilience

# Apply configurations
kubectl apply -f kubernetes/configmap.yaml -n resilience
kubectl apply -f kubernetes/secret.yaml -n resilience
kubectl apply -f kubernetes/deployment.yaml -n resilience
kubectl apply -f kubernetes/service.yaml -n resilience
kubectl apply -f kubernetes/ingress.yaml -n resilience

# Verify deployment
kubectl get pods -n resilience
kubectl get services -n resilience

# View logs
kubectl logs -f deployment/resilience-pipeline -n resilience
```

## Performance Tuning

### Thread Pool Settings

```csharp
// Program.cs
ThreadPool.GetMinThreads(out var workerThreads, out var ioThreads);
ThreadPool.SetMinThreads(
    Math.Max(workerThreads, Environment.ProcessorCount * 2),
    Math.Max(ioThreads, Environment.ProcessorCount * 2)
);
```

### Connection Pool Configuration

```csharp
services.AddResiliencePipeline(builder =>
{
    builder.WithBulkhead("database", 
        maxParallelization: Environment.ProcessorCount * 4,
        maxQueueLength: Environment.ProcessorCount * 8);
});
```

### Memory Optimization

```csharp
// Reduce history retention
services.Configure<ResilienceOptions>(options =>
{
    options.MaxHistoryRecords = 10000;
    options.HistoryRetentionDays = 7;
});
```

## Monitoring and Observability

### Application Insights Integration

```csharp
services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
});

services.AddResiliencePipeline(builder =>
{
    // Pipeline configuration
});
```

### Prometheus Metrics

```csharp
services.AddSingleton<IMetricsCollector, PrometheusMetricsCollector>();

// prometheus.yml
global:
  scrape_interval: 15s

scrape_configs:
  - job_name: 'resilience-pipeline'
    static_configs:
      - targets: ['localhost:9090']
```

### Health Checks

```csharp
builder.Services.AddHealthChecks()
    .AddCheck<ResiliencePipelineHealthCheck>("pipeline");

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", 
    new HealthCheckOptions { Predicate = check => !check.Tags.Contains("startup") });
```

## Security Considerations

### Authentication and Authorization

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = configuration["Auth:Authority"];
        options.Audience = configuration["Auth:Audience"];
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("resilience-admin", policy =>
        policy.RequireRole("Admin"));
});
```

### Rate Limiting

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
        httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.User.Identity?.Name ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                factory: partition => new FixedWindowRateLimiterOptions
                {
                    AutoReplenishment = true,
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1)
                }));
});
```

### SSL/TLS Configuration

```yaml
# Kubernetes - TLS Secret
kubectl create secret tls resilience-tls \
  --cert=path/to/cert.crt \
  --key=path/to/key.key \
  -n resilience

# Ingress with TLS
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: resilience-ingress-tls
spec:
  tls:
  - hosts:
    - api.example.com
    secretName: resilience-tls
  rules:
  - host: api.example.com
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: resilience-pipeline-service
            port:
              number: 80
```

## Backup and Disaster Recovery

### Backup Strategy

```bash
# Database backup (PostgreSQL)
docker exec postgres pg_dump -U postgres resilience > backup.sql

# Restore from backup
docker exec -i postgres psql -U postgres resilience < backup.sql
```

### Failover Configuration

```yaml
# Kubernetes - Pod Disruption Budget
apiVersion: policy/v1
kind: PodDisruptionBudget
metadata:
  name: resilience-pdb
spec:
  minAvailable: 2
  selector:
    matchLabels:
      app: resilience-pipeline
```

## Scaling Strategy

### Horizontal Scaling

```bash
# Scale deployment
kubectl scale deployment resilience-pipeline --replicas=5 -n resilience

# Autoscaling
kubectl autoscale deployment resilience-pipeline \
  --min=2 --max=10 \
  --cpu-percent=80 \
  -n resilience
```

### Load Balancing

Configure session affinity if needed:

```yaml
apiVersion: v1
kind: Service
metadata:
  name: resilience-pipeline-service
spec:
  sessionAffinity: ClientIP
  sessionAffinityConfig:
    clientIP:
      timeoutSeconds: 3600
```

## Troubleshooting Deployment

### Common Issues

**Container won't start:**
```bash
docker logs resilience-pipeline
# Check: image exists, volume permissions, port availability
```

**Kubernetes pod pending:**
```bash
kubectl describe pod <pod-name> -n resilience
# Check: resource requests, node capacity, image pull secrets
```

**Performance degradation:**
```bash
# Monitor resource usage
kubectl top nodes
kubectl top pods -n resilience

# Check metrics
kubectl logs deployment/resilience-pipeline -n resilience | grep -i performance
```

## Production Checklist

- [ ] Database backups configured
- [ ] Monitoring and alerting enabled
- [ ] Health checks implemented
- [ ] Rate limiting configured
- [ ] SSL/TLS certificates valid
- [ ] Security patches applied
- [ ] Load balancing verified
- [ ] Disaster recovery tested
- [ ] Documentation updated
- [ ] Team trained on deployment

## Maintenance Schedule

- **Daily:** Monitor logs and alerts
- **Weekly:** Review metrics and performance
- **Monthly:** Security updates and patches
- **Quarterly:** Disaster recovery drills
- **Annually:** Capacity planning review
