#!/bin/zsh
set -e

CLUSTER_NAME="ats"
KIND_CONFIG="kind-config.yaml"
VALUES="ingress-kind-values.yaml"

# Все образы ingress, которые хотим предзагрузить
INGRESS_IMAGES=(
  "registry.k8s.io/ingress-nginx/controller:v1.14.0"
  "registry.k8s.io/ingress-nginx/kube-webhook-certgen:v1.4.4"
)

# Хелпер: форматировать время в mm:ss
format_time() {
  local T=$1
  printf "%02d:%02d" "$((T/60))" "$((T%60))"
}

# Ассоциативный массив для таймингов
typeset -A TIMES

# Функция замера времени
measure() {
  local name=$1
  local command=$2

  echo "=== [$name] стартует ==="
  local start=$(date +%s)

  eval "$command"

  local end=$(date +%s)
  local delta=$((end - start))
  TIMES["$name"]=$delta

  echo "=== [$name] завершено за $(format_time $delta) ==="
  echo
}

########################################
# 1. Генерация kind-config.yaml
########################################

measure "Генерация_kind_config" "
cat > $KIND_CONFIG <<EOF
kind: Cluster
apiVersion: kind.x-k8s.io/v1alpha4
name: $CLUSTER_NAME
nodes:
  - role: control-plane
    extraPortMappings:
      - containerPort: 80
        hostPort: 9080
        protocol: TCP
      - containerPort: 443
        hostPort: 9443
        protocol: TCP
    kubeadmConfigPatches:
      - |
        kind: InitConfiguration
        nodeRegistration:
          kubeletExtraArgs:
            node-labels: \"ingress-ready=true\"
  - role: worker
EOF
"

########################################
# 2. Создание кластера
########################################

measure "Создание_kind_кластера" "
kind create cluster --config \"$KIND_CONFIG\"
"

########################################
# 3. Ожидание CoreDNS
########################################

measure "Ожидание_появления_coredns" "
until kubectl -n kube-system get deploy coredns >/dev/null 2>&1; do
  echo \"   coredns еще не создан, ждем 3 секунды...\"
  sleep 3
done
"

measure "Ожидание_готовности_coredns" "
kubectl -n kube-system rollout status deploy/coredns --timeout=180s
"

########################################
# 4. Предзагрузка всех образов ingress
########################################

measure "Предзагрузка_ingress_образов" "
for img in \${INGRESS_IMAGES[@]}; do
  echo \"--- Обработка образа: \$img\"
  if ! docker image inspect \$img >/dev/null 2>&1; then
    echo \"   Образ не найден локально, качаем...\"
    docker pull \$img
  else
    echo \"   Образ уже есть локально, пропускаем docker pull\"
  fi

  echo \"   Загружаем образ в kind-ноду...\"
  kind load docker-image \$img --name $CLUSTER_NAME
done
"

########################################
# 5. Генерация values ingress-nginx
########################################

measure "Генерация_values_ingress" "
cat > $VALUES <<EOF
controller:
  kind: DaemonSet

  hostPort:
    enabled: true

  nodeSelector:
    ingress-ready: \"true\"

  tolerations:
    - key: \"node-role.kubernetes.io/control-plane\"
      operator: \"Exists\"
      effect: \"NoSchedule\"

  affinity:
    nodeAffinity:
      requiredDuringSchedulingIgnoredDuringExecution:
        nodeSelectorTerms:
        - matchExpressions:
          - key: ingress-ready
            operator: In
            values:
            - \"true\"

  service:
    type: NodePort
    nodePorts:
      http: 30080
      https: 30443

  admissionWebhooks:
    patch:
      enabled: true

  ingressClassResource:
    default: true
EOF
"

########################################
# 6. Установка ingress-nginx
########################################

measure "Установка_ingress_nginx" "
helm repo add ingress-nginx https://kubernetes.github.io/ingress-nginx >/dev/null
helm repo update >/dev/null

helm install ingress-nginx ingress-nginx/ingress-nginx \
  -n ingress-nginx \
  --create-namespace \
  -f \"$VALUES\"
"

########################################
# 7. Ожидание ingress controller
########################################

measure "Ожидание_ingress_controller" "
kubectl wait --for=condition=ready pod -n ingress-nginx \
  -l app.kubernetes.io/component=controller --timeout=300s
"

########################################
# Итог
########################################

echo
echo "==============================================="
echo "        ВРЕМЯ ЭТАПОВ СОЗДАНИЯ КЛАСТЕРА"
echo "==============================================="
for key in "${(@k)TIMES}"; do
  printf "%-35s %10s\n" "$key" "$(format_time ${TIMES[$key]})"
done
echo "==============================================="
echo
echo "Кластер готов!"
echo "Проверка: curl -H \"Host: hello.local\" http://localhost:9080"