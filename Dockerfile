#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0-bullseye-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0-bullseye-slim AS build
WORKDIR /src

COPY configure-nuget /configure-nuget

RUN sed -i 's/\r$//g' /configure-nuget \
  && chmod +x /configure-nuget

RUN --mount=type=secret,id=CODEARTIFACT_TOKEN /configure-nuget

COPY . .

COPY NuGet.config NuGet.config
RUN dotnet restore "MusicManager.API.sln"
RUN dotnet build "MusicManager.API.sln" -c Release -o /app/build

COPY ./settings/services-live/default_appsettings.json /src/MusicManager.SyncService/appsettings.json

RUN dotnet publish "/src/MusicManager.SyncService/MusicManager.SyncService.csproj" -c Release -o /app/publish/MusicManager.SyncService
RUN dotnet publish "/src/MusicManager.API/MusicManager.API.csproj" -c Release -o /app/publish/MusicManager.API

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
