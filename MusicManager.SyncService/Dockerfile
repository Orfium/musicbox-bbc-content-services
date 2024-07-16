#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:3.1 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:3.1 AS build
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["MusicManager.SyncService/MusicManager.SyncService.csproj", "MusicManager.SyncService/"]
RUN dotnet restore "MusicManager.SyncService/MusicManager.SyncService.csproj"
COPY . .
WORKDIR "/src/MusicManager.SyncService"
RUN dotnet build "MusicManager.SyncService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MusicManager.SyncService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MusicManager.SyncService.dll"]