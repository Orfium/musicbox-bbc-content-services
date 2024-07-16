# MusicBox BBC Content Services

This repository contains the source code for the MusicBox BBC Content Services app.

## Prerequisites

Before you can run the app, make sure you have the following installed on your machine:

- docker
- docker-compose

## Getting Started

1. Clone this repository:

    ```shell
    git clone https://github.com/your-username/musicbox-bbc-content-services.git
    ```

2. Navigate to the project directory:

    ```shell
    cd musicbox-bbc-content-services
    ```

3. Create a .env file in the root of the project with the following contents:

    ```dotenv
    ELASTICSEARCH_URL=opensearch:9200
    NPG_CONNECTION='Host=postgres;Port=5432;Database=musicbox_content_services_db;Username=admin;Password=admin'
    METADATA_API_USERNAME=XXX
    METADATA_API_PASSWORD=XXX
    SM_SEARCH_API_USERNAME=XXX
    SM_SEARCH_API_PASSWORD=XXX
    MUSIC_API_USERNAME=XXX
    MUSIC_API_PASSWORD=XXX
    SM_CORE_API_USERNAME=XXX
    SM_CORE_API_PASSWORD=XXX
    PRS_USERNAME=XXX
    PRS_PASSWORD=XXX
    AWS_S3_ACCESS_KEY=XXX
    AWS_S3_SECRET_KEY=XXX
    AWS_S3_ASSET_HUB_ACCESS_KEY=XXX
    AWS_S3_ASSET_HUB_SECRET_KEY=XXX
    SIGNIANT_CONFIGS_USERNAME=XXX
    SIGNIANT_CONFIGS_PASSWORD=XXX
    DELIVERY_DESTINATION_S3_ACCESS_KEY=XXX
    DELIVERY_DESTINATION_S3_SECRET_KEY=XXX
    ASPNETCORE_URLS=http://*:5001
    ```
3. Build the Docker image:

    ```shell
    make build
    ```

4. Spin up docker services:

    ```shell
    make up
    ```