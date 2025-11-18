# ---------------------------
#       BUILD STAGE
# ---------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копируем только нужные csproj, чтобы кэшировать restore
COPY ats-candidate-service/ats-candidate-service.csproj ./ats-candidate-service/
COPY ats.integration.library/ats.integration.library.csproj ./ats.integration.library/
COPY ats.servicediscovery.library/ats.servicediscovery.library.csproj ./ats.servicediscovery.library/

# ВАЖНО: restore по csproj сервиса
RUN dotnet restore ./ats-candidate-service/ats-candidate-service.csproj

# Теперь копируем весь код решения
COPY . .

# Публикуем сервис (БЕЗ --no-restore, чтобы не ловить NETSDK1064)
WORKDIR /src/ats-candidate-service
RUN dotnet publish -c Release -o /app

# ---------------------------
#       RUNTIME STAGE
# ---------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app ./

EXPOSE 8080

ENTRYPOINT ["dotnet", "ats-candidate-service.dll"]