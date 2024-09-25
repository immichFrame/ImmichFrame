# dev:
# 	docker compose -f ./docker/docker-compose.dev.yml up --remove-orphans || make dev-down

dev:
	dotnet run --project ./ImmichFrame.WebApi

docker-dev:
	docker build . --target dev -t ghcr.io/immichframe/immichframe:dev

dev-down:
	docker compose -f ./docker/docker-compose.dev.yml down --remove-orphans

dev-update:
	docker compose -f ./docker/docker-compose.dev.yml up --build -V --remove-orphans

docker-prod:
	docker build . --target final -t ghcr.io/immichframe/immichframe:main
	
prod:
	docker compose -f ./docker/docker-compose.yml up --build -V --remove-orphans

docker-prune:
	docker system prune -a
