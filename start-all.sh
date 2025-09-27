#!/bin/bash
set -e  

SERVICES=(
  "ats-candidate-service"
  "ats-vacancy-service"

)

for service in "${SERVICES[@]}"; do
  echo "🚀 Запуск docker-compose в $service ..."
  (cd "$service" && docker-compose up -d)
done

echo "✅ Все сервисы запущены!"