#!/bin/bash

set -e

CLUSTER_NAME="ats"

echo "=== Удаляем kind кластер ==="
kind delete cluster --name "$CLUSTER_NAME" || true

echo "=== Удаляем зависшие namespaces (если есть) ==="
for ns in ingress-nginx cert-manager; do
  if kubectl get ns $ns >/dev/null 2>&1; then
    echo "NS $ns существует, удаляем..."
    kubectl delete ns $ns --force --grace-period=0 || true
  fi
done

echo "=== Чистим finalizers у удалённых NS (если зависли) ==="
for ns in ingress-nginx cert-manager; do
  if kubectl get ns $ns >/dev/null 2>&1; then
    echo "Принудительное завершение NS $ns"
    kubectl get ns $ns -o json | \
      jq 'del(.spec.finalizers)' | \
      kubectl replace --raw "/api/v1/namespaces/$ns/finalize" -f - || true
  fi
done

echo "=== Кластер и хвосты удалены успешно ==="