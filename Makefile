.PHONY: docs
.PHONY: immichFrame.Web

dev:
	dotnet run --project ./ImmichFrame.WebApi

test:
	dotnet test ./ImmichFrame.WebApi.Tests/ImmichFrame.WebApi.Tests.csproj

docs:
	npm --prefix docs run start

api:
	npm --prefix immichFrame.Web run api

docker-build-prod:
	docker buildx build --platform linux/amd64 --no-cache . --target final -t ghcr.io/immichframe/immichframe:latest --build-arg VERSION=1.0.0.0
	
docker-prod:
	docker compose -f ./docker/docker-compose.yml up --build -V --remove-orphans

docker-prune:
	docker system prune -a
