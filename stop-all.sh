#!/bin/bash
set -e

SERVICES=(
  "ats-candidate-service"
  "ats-vacancy-service"
  "ats-recruitment-service"
  "ats-interview-service"
  "ats-authorization-service"
  "rabbitmq"
  "keycloak"
  "ats-service-discovery"
)

for service in "${SERVICES[@]}"; do
  echo "🛑 Остановка docker-compose в $service ..."
  (cd "$service" && docker-compose down)
done

echo "✅ Все сервисы остановлены!"