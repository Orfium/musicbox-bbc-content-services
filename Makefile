help:
	@echo "Please use 'make <target>' where <target> is one of the following:"
	@echo "  build to build docker image"
	@echo "  configure nuget code artifact source"
	@echo "  restore solution"
	@echo "  up to start docker services"


build:
	export CODEARTIFACT_TOKEN=$(shell aws-vault exec musicboxdev@smadl1 -- aws codeartifact get-authorization-token --region eu-west-2 --domain musicbox --domain-owner 462711359312 --duration-seconds 900 --query authorizationToken --output text) && \
	docker buildx bake --no-cache

nuget-config-code-artifact:
	(dotnet nuget remove source musicbox/mbmt-nuget || true) && \
	export CODEARTIFACT_TOKEN=$(shell aws-vault exec musicboxdev@smadl1 -- aws codeartifact get-authorization-token --region eu-west-2 --domain musicbox --domain-owner 462711359312 --duration-seconds 900 --query authorizationToken --output text) && \
	dotnet nuget add source https://musicbox-462711359312.d.codeartifact.eu-west-2.amazonaws.com/nuget/mbmt-nuget/v3/index.json --name musicbox/mbmt-nuget --password $$CODEARTIFACT_TOKEN --username aws --store-password-in-clear-text
	
nuget-restore: nuget-config-code-artifact
	dotnet restore MusicManager.API.sln

up:
	docker-compose up -d
