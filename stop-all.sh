#!/bin/bash
set -e

SERVICES=(
  "ats-candidate-service"
  "ats-vacancy-service"
)

for service in "${SERVICES[@]}"; do
  echo "🛑 Остановка docker-compose в $service ..."
  (cd "$service" && docker-compose down)
done

echo "✅ Все сервисы остановлены!"