SHELL := /bin/zsh

COMPOSE_FILE := compose.yaml
APP_PROJECT := MartenNine.App/MartenNine.App.csproj
POSTGRES_CONTAINER := marten-nine-postgres
MARTEN_CONNECTION_STRING ?= Host=localhost;Port=5432;Database=marten_nine;Username=postgres;Password=postgres

.PHONY: run up wait-db app down logs

run: up wait-db app

up:
	docker compose -f $(COMPOSE_FILE) up -d postgres

wait-db:
	until [[ "$$(docker inspect -f '{{.State.Health.Status}}' $(POSTGRES_CONTAINER) 2>/dev/null)" == "healthy" ]]; do \
		echo "waiting for postgres..."; \
		sleep 1; \
	done

app:
	MARTEN_CONNECTION_STRING='$(MARTEN_CONNECTION_STRING)' dotnet run --project $(APP_PROJECT)

down:
	docker compose -f $(COMPOSE_FILE) down

logs:
	docker compose -f $(COMPOSE_FILE) logs -f postgres
