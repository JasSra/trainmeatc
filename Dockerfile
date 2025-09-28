# TrainMeATC - Production Docker Build
# Multi-stage build for optimized production deployment

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files and restore dependencies
COPY backend/PilotSim.sln ./
COPY backend/PilotSim.Core/PilotSim.Core.csproj ./PilotSim.Core/
COPY backend/PilotSim.Data/PilotSim.Data.csproj ./PilotSim.Data/
COPY backend/PilotSim.Server/PilotSim.Server.csproj ./PilotSim.Server/
COPY backend/PilotSim.Tests/PilotSim.Tests.csproj ./PilotSim.Tests/

RUN dotnet restore

# Copy source code and build
COPY backend/ ./
RUN dotnet build -c Release --no-restore

# Test stage (optional - can be skipped in production builds)
FROM build AS test
RUN dotnet test --no-build --verbosity normal

# Publish stage
FROM build AS publish
RUN dotnet publish PilotSim.Server/PilotSim.Server.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Create non-root user for security
RUN addgroup --system --gid 1001 trainmeatc \
    && adduser --system --uid 1001 --ingroup trainmeatc trainmeatc

# Install required packages
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        curl \
        ca-certificates \
    && rm -rf /var/lib/apt/lists/*

# Copy published application
COPY --from=publish /app/publish .

# Create directories for data and audio with proper permissions
RUN mkdir -p /app/data /app/wwwroot/audio \
    && chown -R trainmeatc:trainmeatc /app

# Switch to non-root user
USER trainmeatc

# Configure environment
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_HTTP_PORTS=8080
ENV DOTNET_EnableDiagnostics=0

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Expose port
EXPOSE 8080

# Start application
ENTRYPOINT ["dotnet", "PilotSim.Server.dll"]