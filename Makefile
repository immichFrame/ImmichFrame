.PHONY: docs
.PHONY: immichFrame.Web

dev:
	dotnet run --project ./ImmichFrame.WebApi

docs:
	npm --prefix docs run start

api:
	npm --prefix immichFrame.Web run api

docker-dev:
	docker build . --target dev -t ghcr.io/immichframe/immichframe:dev

dev-down:
	docker compose -f ./docker/docker-compose.dev.yml down --remove-orphans

dev-update:
	docker compose -f ./docker/docker-compose.dev.yml up --build -V --remove-orphans

docker-prod:
	docker buildx build --platform linux/amd64 --no-cache . --target final -t ghcr.io/immichframe/immichframe:main
	
prod:
	docker compose -f ./docker/docker-compose.yml up --build -V --remove-orphans

docker-prune:
	docker system prune -a
