# ============================
# BUILD STAGE
# ============================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution file and project files for better layer caching
COPY ats.sln .
COPY ats-recruitment-service/ats-recruitment-service.csproj ./ats-recruitment-service/
COPY ats.integration.library/ats.integration.library.csproj ./ats.integration.library/
COPY ats.servicediscovery.library/ats.servicediscovery.library.csproj ./ats.servicediscovery.library/

# Restore dependencies
RUN dotnet restore "ats-recruitment-service/ats-recruitment-service.csproj"

# Copy remaining source code
COPY . .

# Build and publish
WORKDIR /src/ats-recruitment-service
RUN dotnet publish -c Release -o /app/publish --no-restore

# ============================
# RUNTIME STAGE
# ============================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create a non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Copy published files
COPY --from=build /app/publish .

# Change ownership to non-root user
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Set environment variables
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "ats-recruitment-service.dll"]
