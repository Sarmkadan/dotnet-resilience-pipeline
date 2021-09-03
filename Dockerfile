# Multi-stage build for DotNet Resilience Pipeline

# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS builder
WORKDIR /src

# Copy project files
COPY DotNetResiliencePipeline.csproj .
RUN dotnet restore

# Copy all source files
COPY src/ ./src/

# Build the library
RUN dotnet build -c Release --no-restore -o /app/build

# Stage 2: Publish
FROM builder AS publisher
RUN dotnet publish -c Release --no-restore -o /app/publish

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app

# Copy published files
COPY --from=publisher /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
  CMD dotnet --version || exit 1

# Labels
LABEL maintainer="Vladyslav Zaiets <rutova2@gmail.com>"
LABEL description="DotNet Resilience Pipeline - Production-grade resilience patterns library"
LABEL version="1.0.0"

# Entry point
ENTRYPOINT ["dotnet"]
CMD ["DotNetResiliencePipeline.dll"]
