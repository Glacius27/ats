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

echo "🧹 Очистка всех volumes в локальной среде..."

for service in "${SERVICES[@]}"; do
  echo "🗑  Удаление volumes в $service ..."

  if [ -d "$service" ]; then
    pushd "$service" > /dev/null

    # Проверяем, есть ли docker-compose.yml
    if [ -f "docker-compose.yml" ]; then
      # Удаляем volumes, связанные с этим compose
      docker compose down -v --remove-orphans
      echo "✅ Volumes для $service удалены"
    else
      echo "⚠️  docker-compose.yml не найден в $service, пропускаем..."
    fi

    popd > /dev/null
  else
    echo "⚠️  Директория $service не найдена, пропускаем..."
  fi
done

echo "🧼 Все volumes удалены успешно!"