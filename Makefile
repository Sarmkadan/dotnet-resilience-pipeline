.PHONY: help build test clean restore run docker-build docker-run examples all

# Colors
GREEN := \033[0;32m
YELLOW := \033[0;33m
NC := \033[0m # No Color

# Default target
help:
	@echo "$(GREEN)DotNet Resilience Pipeline - Build Targets$(NC)"
	@echo ""
	@echo "$(YELLOW)Common targets:$(NC)"
	@echo "  make build           - Build the project"
	@echo "  make test            - Run tests"
	@echo "  make clean           - Clean build artifacts"
	@echo "  make restore         - Restore dependencies"
	@echo "  make run             - Run the application"
	@echo "  make pack            - Create NuGet package"
	@echo ""
	@echo "$(YELLOW)Docker targets:$(NC)"
	@echo "  make docker-build    - Build Docker image"
	@echo "  make docker-run      - Run Docker container"
	@echo "  make docker-compose  - Start services with docker-compose"
	@echo ""
	@echo "$(YELLOW)Development targets:$(NC)"
	@echo "  make examples        - Run all examples"
	@echo "  make format          - Format code with dotnet format"
	@echo "  make lint            - Run code analysis"
	@echo ""
	@echo "$(YELLOW)Utility targets:$(NC)"
	@echo "  make all             - Build, test, and pack"
	@echo "  make help            - Show this help message"
	@echo ""

# Project variables
PROJECT_NAME := DotNetResiliencePipeline
CONFIGURATION := Release
DOTNET_VERSION := 10.0
DOCKER_IMAGE := sarmkadan/dotnet-resilience-pipeline
DOCKER_TAG := latest

# Build targets
build:
	@echo "$(GREEN)Building project...$(NC)"
	dotnet build -c $(CONFIGURATION)

rebuild: clean build
	@echo "$(GREEN)Rebuild complete$(NC)"

clean:
	@echo "$(GREEN)Cleaning artifacts...$(NC)"
	dotnet clean -c $(CONFIGURATION)
	rm -rf bin/ obj/ publish/
	@echo "$(GREEN)Clean complete$(NC)"

restore:
	@echo "$(GREEN)Restoring dependencies...$(NC)"
	dotnet restore

# Testing targets
test:
	@echo "$(GREEN)Running tests...$(NC)"
	dotnet test -c $(CONFIGURATION) --no-build

test-verbose:
	@echo "$(GREEN)Running tests (verbose)...$(NC)"
	dotnet test -c $(CONFIGURATION) --no-build -v detailed

# Packaging targets
pack:
	@echo "$(GREEN)Creating NuGet package...$(NC)"
	dotnet pack -c $(CONFIGURATION) -o ./nupkg
	@echo "$(GREEN)Package created in ./nupkg$(NC)"

publish:
	@echo "$(GREEN)Publishing library...$(NC)"
	dotnet publish -c $(CONFIGURATION) -o ./publish

# Code quality targets
format:
	@echo "$(GREEN)Formatting code...$(NC)"
	dotnet format

lint:
	@echo "$(GREEN)Running code analysis...$(NC)"
	dotnet build -c $(CONFIGURATION) /p:EnforceCodeStyleInBuild=true

# Docker targets
docker-build:
	@echo "$(GREEN)Building Docker image...$(NC)"
	docker build -t $(DOCKER_IMAGE):$(DOCKER_TAG) .
	@echo "$(GREEN)Docker image built: $(DOCKER_IMAGE):$(DOCKER_TAG)$(NC)"

docker-run: docker-build
	@echo "$(GREEN)Running Docker container...$(NC)"
	docker run -p 5000:5000 --name $(PROJECT_NAME) $(DOCKER_IMAGE):$(DOCKER_TAG)

docker-compose:
	@echo "$(GREEN)Starting services with docker-compose...$(NC)"
	docker-compose up -d
	@echo "$(GREEN)Services started$(NC)"
	@echo "$(YELLOW)Available services:$(NC)"
	@echo "  Application: http://localhost:5000"
	@echo "  Prometheus: http://localhost:9090"
	@echo "  Grafana: http://localhost:3000"
	@echo "  pgAdmin: http://localhost:5050"

docker-compose-down:
	@echo "$(GREEN)Stopping services...$(NC)"
	docker-compose down

docker-compose-logs:
	docker-compose logs -f app

docker-clean:
	@echo "$(GREEN)Cleaning Docker resources...$(NC)"
	docker rmi $(DOCKER_IMAGE):$(DOCKER_TAG)
	docker-compose down -v

# Example targets
examples:
	@echo "$(GREEN)Building examples...$(NC)"
	cd examples && dotnet build -c $(CONFIGURATION)
	@echo "$(GREEN)Running BasicUsage example...$(NC)"
	cd examples && dotnet run --project BasicUsage.cs

run:
	@echo "$(GREEN)Running application...$(NC)"
	dotnet run -c $(CONFIGURATION)

# Development workflow
dev: restore build
	@echo "$(GREEN)Development build complete$(NC)"

dev-watch:
	@echo "$(GREEN)Watching for changes...$(NC)"
	dotnet watch run

# Comprehensive targets
all: restore build test pack
	@echo "$(GREEN)All targets complete$(NC)"

ci: restore build test lint
	@echo "$(GREEN)CI pipeline complete$(NC)"

# Cleaning targets
clean-all: clean docker-clean
	@echo "$(GREEN)Complete cleanup done$(NC)"

# Documentation
docs:
	@echo "$(GREEN)Documentation:$(NC)"
	@echo "  - README.md: Project overview and quick start"
	@echo "  - docs/getting-started.md: Getting started guide"
	@echo "  - docs/architecture.md: Architecture documentation"
	@echo "  - docs/api-reference.md: Complete API reference"
	@echo "  - docs/deployment.md: Deployment guide"
	@echo "  - docs/faq.md: Frequently asked questions"

# Version info
version:
	@echo "$(GREEN)Project Info:$(NC)"
	@echo "  Name: $(PROJECT_NAME)"
	@echo "  Target Framework: .NET $(DOTNET_VERSION)"
	@echo "  Configuration: $(CONFIGURATION)"
	@echo "  Docker Image: $(DOCKER_IMAGE):$(DOCKER_TAG)"
	@dotnet --version

# Debugging
debug:
	@echo "$(GREEN)Building in Debug configuration...$(NC)"
	dotnet build -c Debug

debug-watch:
	@echo "$(GREEN)Watching in Debug mode...$(NC)"
	dotnet watch run -c Debug

# CI/CD
ci-build:
	@echo "$(GREEN)CI: Building...$(NC)"
	dotnet build -c Release /p:ContinuousIntegrationBuild=true

ci-test:
	@echo "$(GREEN)CI: Testing...$(NC)"
	dotnet test -c Release --no-build --logger trx

ci-pack:
	@echo "$(GREEN)CI: Packing...$(NC)"
	dotnet pack -c Release -o ./artifacts

# Print variables
print-%:
	@echo $* = $($*)

.PHONY: help build rebuild clean restore test test-verbose pack publish
.PHONY: format lint docker-build docker-run docker-compose docker-clean
.PHONY: examples run dev dev-watch all ci clean-all docs version
.PHONY: debug debug-watch ci-build ci-test ci-pack print-%
