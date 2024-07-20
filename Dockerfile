#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0-bullseye-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0-bullseye-slim AS build
WORKDIR /src

COPY configure-nuget /configure-nuget

RUN sed -i 's/\r$//g' /configure-nuget \
  && chmod +x /configure-nuget

# Copy solution and csproj files
COPY MusicManager.API.sln .
COPY MusicManager.SyncService/MusicManager.SyncService.csproj ./MusicManager.SyncService/MusicManager.SyncService.csproj
COPY MusicManager.API/MusicManager.API.csproj ./MusicManager.API/MusicManager.API.csproj
COPY MusicManager.Core/MusicManager.Core.csproj ./MusicManager.Core/MusicManager.Core.csproj
COPY MusicManager.Application/MusicManager.Application.csproj ./MusicManager.Application/MusicManager.Application.csproj
COPY Elasticsearch/Elasticsearch.csproj ./Elasticsearch/Elasticsearch.csproj
COPY MusicManager.Logics/MusicManager.Logics.csproj ./MusicManager.Logics/MusicManager.Logics.csproj
COPY MusicManager.UserSync/MusicManager.UserSync.csproj ./MusicManager.UserSync/MusicManager.UserSync.csproj
COPY MusicManager.PrsSearch/MusicManager.PrsSearch.csproj ./MusicManager.PrsSearch/MusicManager.PrsSearch.csproj
COPY MusicManager.ExternalAPI/MusicManager.ExternalAPI.csproj ./MusicManager.ExternalAPI/MusicManager.ExternalAPI.csproj
COPY MusicManager.SupportApp/MusicManager.SupportApp.csproj ./MusicManager.SupportApp/MusicManager.SupportApp.csproj
COPY LabelProcess/LabelProcess.csproj ./LabelProcess/LabelProcess.csproj
COPY MLCheckAPI/MLCheckAPI.csproj ./MLCheckAPI/MLCheckAPI.csproj
COPY MusicManager.Playout/MusicManager.Playout.csproj ./MusicManager.Playout/MusicManager.Playout.csproj
COPY MusicManager.Infrastructure/MusicManager.Infrastructure.csproj ./MusicManager.Infrastructure/MusicManager.Infrastructure.csproj
COPY MusicManager.InitDB/MusicManager.InitDB.csproj ./MusicManager.InitDB/MusicManager.InitDB.csproj

# configure nuget with code artifact source
RUN --mount=type=secret,id=CODEARTIFACT_TOKEN /configure-nuget

# Restore solution
RUN dotnet restore "MusicManager.API.sln"

# copy everything else and build app
COPY . .
RUN sed -i 's/\r$//g' /src/start.sh \
  && chmod +x /src/start.sh

RUN dotnet publish "/src/MusicManager.SyncService/MusicManager.SyncService.csproj" -c Release -o /app/publish/MusicManager.SyncService
RUN dotnet publish "/src/MusicManager.API/MusicManager.API.csproj" -c Release -o /app/publish/MusicManager.API
RUN dotnet publish "/src/MusicManager.InitDB/MusicManager.InitDB.csproj" -c Release -o /app/publish/MusicManager.InitDB

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY --from=build /src/settings ./settings
COPY --from=build /src/start.sh /app/start.sh
