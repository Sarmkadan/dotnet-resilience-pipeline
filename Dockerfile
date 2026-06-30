# Multi-stage build for DotNet Resilience Pipeline

# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY DotNetResiliencePipeline.csproj .
RUN dotnet restore

# Copy source files and build
COPY . .
RUN dotnet build -c Release --no-restore

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish -c Release --no-restore -o /app/publish /p:UseAppHost=false

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app

# Create non-root user
RUN addgroup --system --gid 1001 appgroup && \
    adduser --system --uid 1001 --ingroup appgroup appuser

# Copy published output
COPY --from=publish /app/publish .

# Run as non-root
USER appuser

ENTRYPOINT ["dotnet", "DotNetResiliencePipeline.dll"]
