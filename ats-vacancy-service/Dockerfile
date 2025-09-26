FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "ats-vacancy-service.csproj"
RUN dotnet build "ats-vacancy-service.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ats-vacancy-service.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ats-vacancy-service.dll"]