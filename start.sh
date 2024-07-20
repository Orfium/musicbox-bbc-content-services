#!/bin/bash

set -o errexit
set -o pipefail
set -o nounset


if [ $# -ne 2 ]
  then
    echo "`basename ${0}`:usage: [command] [env]"
    exit 1
fi

command="${1}"
env="${2}"


if [ "${env:-}" != "dev" ] && [ "${env:-}" != "live" ]; then
    echo "Invalid environment: ${env}"
    exit 1
fi

SERVICES_START_COMMAND="dotnet MusicManager.SyncService/MusicManager.SyncService.dll"
API_START_COMMAND="dotnet MusicManager.API/MusicManager.API.dll"

case $command in
   "ml-content-api") cp /app/settings/contentapi-${env}/contentapi_appsettings.json /app/appsettings.json && ${API_START_COMMAND}
   ;;
   "ml-uploader-service") cp /app/settings/services-${env}/uploader_service_appsettings.json /app/appsettings.json && ${SERVICES_START_COMMAND}
   ;;
   "ml-track-sync-service") cp /app/settings/services-${env}/track_sync_service_appsettings.json /app/appsettings.json && ${SERVICES_START_COMMAND}
   ;;
   "ml-playout-service") cp /app/settings/services-${env}/playout_service_appsettings.json /app/appsettings.json && ${SERVICES_START_COMMAND}
   ;;
   "ml-master-ws-sync-service") cp /app/settings/services-${env}/master_ws_sync_service_appsettings.json /app/appsettings.json && ${SERVICES_START_COMMAND}
   ;;
   "ml-nighttime-service") cp /app/settings/services-${env}/nighttime_service_appsettings.json /app/appsettings.json && ${SERVICES_START_COMMAND}
   ;;
   "ml-workspace-lib-sync-service") cp /app/settings/services-${env}/workspace_lib_sync_service_appsettings.json /app/appsettings.json && ${SERVICES_START_COMMAND}
   ;;
   *) echo "Uknown [command]: $command"
      exit 1 # Command to come out of the program with status 1
   ;;
esac
