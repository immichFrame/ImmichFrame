dev:
	docker compose -f ../docker/docker-compose.dev.yml up --remove-orphans || make dev-down

dev-down:
	docker compose -f ../docker/docker-compose.dev.yml down --remove-orphans

dev-update:
	docker compose -f ../docker/docker-compose.dev.yml up --build -V --remove-orphans

prod:
	docker compose -f ../docker/docker-compose.prod.yml up --build -V --remove-orphans

api:
	npm run api

attach-server:
	docker exec -it docker_immich-server_1 sh

format-%:
	npm --prefix $(subst sdk,open-api/typescript-sdk,$*) run | grep 'format:fix' >/dev/null \
		&& npm --prefix $(subst sdk,open-api/typescript-sdk,$*) run format:fix || true
lint-%:
	npm --prefix $* run lint:fix

clean:
	find . -name "node_modules" -type d -prune -exec rm -rf '{}' +
	find . -name "dist" -type d -prune -exec rm -rf '{}' +
	find . -name "build" -type d -prune -exec rm -rf '{}' +
	find . -name "svelte-kit" -type d -prune -exec rm -rf '{}' +
	docker compose -f ./docker/docker-compose.dev.yml rm -v -f || true
	docker compose -f ./e2e/docker-compose.yml rm -v -f || true
