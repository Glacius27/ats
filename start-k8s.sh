#!/bin/bash

# Comprehensive script to deploy all ATS infrastructure, observability, microservices, and frontend to local K8s cluster
# Usage: ./start-k8s.sh [namespace]

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$SCRIPT_DIR"
NAMESPACE="${1:-ats}"

# Colors for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Service lists
INFRA_SERVICES=("postgres" "mongo" "redis" "rabbitmq" "keycloak" "minio")
MICROSERVICES=("ats-authorization-service" "ats-candidate-service" "ats-interview-service" "ats-recruitment-service" "ats-vacancy-service")
FRONTEND_SERVICE="ats-frontend"

echo -e "${BLUE}╔══════════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║   ATS Kubernetes Deployment Script                        ║${NC}"
echo -e "${BLUE}║   Deploying: Infrastructure + Observability + Services    ║${NC}"
echo -e "${BLUE}╚══════════════════════════════════════════════════════════════╝${NC}"
echo ""
echo -e "${BLUE}Namespace: ${NAMESPACE}${NC}"
echo ""

# ============================================================================
# Prerequisites Check
# ============================================================================
echo -e "${YELLOW}📋 Step 1: Checking prerequisites...${NC}"

check_command() {
    if ! command -v "$1" &> /dev/null; then
        echo -e "${RED}❌ $1 is not installed. Please install $1 first.${NC}"
        exit 1
    fi
}

check_command kubectl
check_command helm
check_command docker

# Check if Docker is running
if ! docker info &> /dev/null; then
    echo -e "${RED}❌ Docker is not running. Please start Docker Desktop.${NC}"
    exit 1
fi

# Check if Kubernetes is running
if ! kubectl cluster-info &> /dev/null; then
    echo -e "${RED}❌ Kubernetes cluster is not accessible.${NC}"
    echo -e "${YELLOW}   Please ensure Docker Desktop Kubernetes is enabled:${NC}"
    echo -e "${YELLOW}   Docker Desktop → Settings → Kubernetes → Enable Kubernetes${NC}"
    exit 1
fi

# Verify this is Docker Desktop Kubernetes (not KIND)
if kubectl get nodes -o name 2>/dev/null | grep -q "kind"; then
    echo -e "${YELLOW}⚠️  WARNING: KIND cluster detected. This script is configured for Docker Desktop Kubernetes.${NC}"
    read -p "   Continue anyway? (y/N) " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        exit 1
    fi
fi

echo -e "${GREEN}✅ All prerequisites met${NC}"
echo ""

# ============================================================================
# Create Namespace
# ============================================================================
echo -e "${YELLOW}📦 Step 2: Creating namespace...${NC}"
kubectl create namespace "$NAMESPACE" --dry-run=client -o yaml | kubectl apply -f -
echo -e "${GREEN}✅ Namespace ready${NC}"
echo ""

# ============================================================================
# Install Ingress Controller
# ============================================================================
echo -e "${YELLOW}🌐 Step 3: Setting up Ingress Controller...${NC}"

if ! kubectl get namespace ingress-nginx &> /dev/null; then
    echo "  Installing NGINX Ingress Controller..."
    helm repo add ingress-nginx https://kubernetes.github.io/ingress-nginx 2>/dev/null || true
    helm repo update
    
    helm upgrade --install ingress-nginx ingress-nginx/ingress-nginx \
        --namespace ingress-nginx \
        --create-namespace \
        --set controller.service.type=LoadBalancer \
        --wait --timeout=5m || echo "⚠️  Ingress installation may still be in progress"
    
    echo -e "${GREEN}✅ Ingress controller installed${NC}"
else
    echo -e "${GREEN}✅ Ingress controller already exists${NC}"
fi
echo ""

# ============================================================================
# Build Docker Images
# ============================================================================
echo -e "${YELLOW}🐳 Step 4: Building Docker images...${NC}"

build_service() {
    local service=$1
    local dockerfile_path="${service}/Dockerfile"
    
    if [ ! -f "$dockerfile_path" ]; then
        echo "  ⚠️  Dockerfile not found: $dockerfile_path (skipping)"
        return 0
    fi
    
    local image_name="ats-${service}:latest"
    if docker image inspect "$image_name" &>/dev/null; then
        echo "  ✅ Image already exists: ${image_name}"
    else
        echo "  🔨 Building ${service}..."
        docker build -f "$dockerfile_path" -t "$image_name" "$PROJECT_ROOT" || {
            echo -e "${RED}❌ Failed to build ${service}${NC}"
            exit 1
        }
        echo "  ✅ Built: ${image_name}"
    fi
}

# Build microservices
for service in "${MICROSERVICES[@]}"; do
    build_service "$service"
done

# Build frontend
# echo "  🔨 Building frontend..."
# FRONTEND_IMAGE="ats-${FRONTEND_SERVICE}:latest"
# if docker image inspect "$FRONTEND_IMAGE" &>/dev/null; then
#     echo "  ✅ Image already exists: ${FRONTEND_IMAGE}"
# else
#     # Build frontend with environment variables for Kubernetes
#     docker build \
#         -f ats-frontend/Dockerfile \
#         -t "$FRONTEND_IMAGE" \
#         --build-arg REACT_APP_KEYCLOAK_URL=http://keycloak.ats.local \
#         --build-arg REACT_APP_KEYCLOAK_REALM=ats \
#         --build-arg REACT_APP_KEYCLOAK_CLIENT_ID=ats-frontend \
#         --build-arg REACT_APP_VACANCY_SERVICE_URL=http://vacancy.local \
#         --build-arg REACT_APP_AUTHORIZATION_SERVICE_URL=http://authorization.local \
#         --build-arg REACT_APP_CANDIDATE_SERVICE_URL=http://candidate.local \
#         --build-arg REACT_APP_RECRUITMENT_SERVICE_URL=http://recruitment.local \
#         "$PROJECT_ROOT" || {
#         echo -e "${RED}❌ Failed to build frontend${NC}"
#         exit 1
#     }
#     echo "  ✅ Built: ${FRONTEND_IMAGE}"
# fi

echo -e "${GREEN}✅ All Docker images ready${NC}"
echo ""

# ============================================================================
# Deploy Infrastructure
# ============================================================================
echo -e "${YELLOW}🏗️  Step 5: Deploying Infrastructure (PostgreSQL, MongoDB, Redis, RabbitMQ, Keycloak, MinIO)...${NC}"
cd "$PROJECT_ROOT/helm/ats-infra"

helm upgrade --install ats-infra . \
    --namespace "$NAMESPACE" \
    --wait --timeout=10m || echo "⚠️  Infrastructure deployment may still be in progress"

echo "⏳ Waiting for infrastructure to be ready..."
sleep 30

# Wait for databases to be ready
echo -e "${YELLOW}🗄️  Waiting for databases to be ready...${NC}"

wait_for_postgres_instance() {
    local instance_name="$1"
    local display_name="$2"
    local ready=false
    
    for i in {1..30}; do
        local pod_name
        pod_name="$(kubectl get pods -n "$NAMESPACE" -l app="${instance_name}-postgres" -o jsonpath='{.items[0].metadata.name}' 2>/dev/null || true)"
        
        if [ -n "$pod_name" ]; then
            if kubectl exec -n "$NAMESPACE" "$pod_name" -- pg_isready -U ats 2>/dev/null; then
                echo "  ✅ ${display_name} PostgreSQL is ready"
                ready=true
                break
            fi
        fi
        
        if [ $((i % 5)) -eq 0 ]; then
            echo "  ⏳ Waiting for ${display_name} PostgreSQL... ($i/30)"
        fi
        sleep 2
    done
    
    if [ "$ready" = false ]; then
        echo "  ⚠️  ${display_name} PostgreSQL did not report ready after 60 seconds. Continuing anyway."
    fi
}

wait_for_postgres_instance "authorization" "Authorization"
wait_for_postgres_instance "candidates" "Candidates"
wait_for_postgres_instance "recruitment" "Recruitment"
wait_for_postgres_instance "interviews" "Interviews"

# Wait for MongoDB
echo "  ⏳ Waiting for MongoDB..."
for i in {1..30}; do
    if kubectl exec -n "$NAMESPACE" deployment/mongo -- mongosh --eval "db.adminCommand('ping')" &>/dev/null 2>&1; then
        echo "  ✅ MongoDB is ready"
        break
    fi
    if [ $i -eq 30 ]; then
        echo "  ⚠️  MongoDB did not report ready after 60 seconds. Continuing anyway."
    fi
    sleep 2
done

echo -e "${GREEN}✅ Infrastructure deployed${NC}"
echo ""

# ============================================================================
# Deploy Observability
# ============================================================================
echo -e "${YELLOW}📊 Step 6: Deploying Observability Stack (Prometheus, Grafana, Loki, Promtail)...${NC}"
cd "$PROJECT_ROOT/helm/ats-observability"

# Add required Helm repositories
helm repo add prometheus-community https://prometheus-community.github.io/helm-charts 2>/dev/null || true
helm repo add grafana https://grafana.github.io/helm-charts 2>/dev/null || true
helm repo update

helm upgrade --install ats-observability . \
    --namespace "$NAMESPACE" \
    --wait --timeout=10m || echo "⚠️  Observability deployment may still be in progress"

# Remove prometheus-node-exporter (not compatible with Docker Desktop on macOS)
echo "  Removing prometheus-node-exporter (not compatible with Docker Desktop)..."
kubectl delete daemonset,serviceaccount,service -n "$NAMESPACE" -l app.kubernetes.io/name=prometheus-node-exporter --ignore-not-found=true 2>/dev/null || true
kubectl delete pod -n "$NAMESPACE" -l app.kubernetes.io/name=prometheus-node-exporter --ignore-not-found=true 2>/dev/null || true

echo -e "${GREEN}✅ Observability stack deployed${NC}"
echo ""

# ============================================================================
# Deploy Microservices
# ============================================================================
echo -e "${YELLOW}🔧 Step 7: Deploying Microservices...${NC}"
cd "$PROJECT_ROOT/helm/ats-services"

for service in "${MICROSERVICES[@]}"; do
    echo "  Deploying ${service}..."
    helm upgrade --install "${service}" "./${service}" \
        --namespace "$NAMESPACE" \
        --set image.repository="ats-${service}" \
        --set image.tag="latest" \
        --set image.pullPolicy="IfNotPresent" \
        --set ingress.enabled=true \
        --wait --timeout=5m || echo "⚠️  ${service} deployment may still be in progress"
done

echo -e "${GREEN}✅ Microservices deployed${NC}"
echo ""

# ============================================================================
# Deploy Frontend
# ============================================================================
echo -e "${YELLOW}🖥️  Step 8: Deploying Frontend...${NC}"

if [ -d "$PROJECT_ROOT/helm/ats-services/${FRONTEND_SERVICE}" ]; then
    helm upgrade --install "${FRONTEND_SERVICE}" "./${FRONTEND_SERVICE}" \
        --namespace "$NAMESPACE" \
        --set image.repository="ats-${FRONTEND_SERVICE}" \
        --set image.tag="latest" \
        --set image.pullPolicy="IfNotPresent" \
        --set ingress.enabled=true \
        --wait --timeout=5m || echo "⚠️  ${FRONTEND_SERVICE} deployment may still be in progress"
    
    echo "  Waiting for ${FRONTEND_SERVICE}..."
    kubectl wait --for=condition=ready pod -l app="${FRONTEND_SERVICE}" -n "$NAMESPACE" --timeout=5m 2>/dev/null || echo "  ⚠️  ${FRONTEND_SERVICE} may still be starting"
    echo -e "${GREEN}✅ Frontend deployed${NC}"
else
    echo -e "${YELLOW}⚠️  Frontend helm chart not found, skipping frontend deployment${NC}"
fi
echo ""

# ============================================================================
# Wait for Services to Stabilize
# ============================================================================
echo -e "${YELLOW}⏳ Step 9: Waiting for services to stabilize and apply migrations...${NC}"
sleep 15

# Force restart services to ensure migrations are applied
echo -e "${YELLOW}🔄 Restarting services to ensure migrations are applied...${NC}"
for service in "${MICROSERVICES[@]}"; do
    echo "  Restarting ${service}..."
    kubectl rollout restart deployment/"${service}" -n "$NAMESPACE" 2>/dev/null || true
done

# Wait for services to be ready after restart
echo "  Waiting for services to be ready after restart..."
sleep 20

for service in "${MICROSERVICES[@]}"; do
    kubectl wait --for=condition=ready pod -l app="${service}" -n "$NAMESPACE" --timeout=3m 2>/dev/null || echo "  ⚠️  ${service} may still be starting"
done

echo -e "${GREEN}✅ Services stabilized${NC}"
echo ""

# ============================================================================
# Update Hosts File
# ============================================================================
echo -e "${YELLOW}📝 Step 10: Updating /etc/hosts file...${NC}"

HOSTS_ENTRIES=(
    "127.0.0.1 authorization.local"
    "127.0.0.1 candidate.local"
    "127.0.0.1 interview.local"
    "127.0.0.1 recruitment.local"
    "127.0.0.1 vacancy.local"
    "127.0.0.1 frontend.local"
    "127.0.0.1 keycloak.ats.local"
    "127.0.0.1 grafana.local"
    "127.0.0.1 prometheus.local"
)

if [ "$EUID" -eq 0 ]; then
    HOSTS_FILE="/etc/hosts"
    # Remove existing ATS entries
    sed -i.bak '/# ATS Services/,/# End ATS Services/d' "$HOSTS_FILE" 2>/dev/null || true
    
    # Add new entries
    echo "" >> "$HOSTS_FILE"
    echo "# ATS Services" >> "$HOSTS_FILE"
    for entry in "${HOSTS_ENTRIES[@]}"; do
        echo "$entry" >> "$HOSTS_FILE"
    done
    echo "# End ATS Services" >> "$HOSTS_FILE"
    echo -e "${GREEN}✅ /etc/hosts updated${NC}"
else
    echo -e "${YELLOW}⚠️  Run with sudo to automatically update /etc/hosts${NC}"
    echo "   Or manually add these entries:"
    for entry in "${HOSTS_ENTRIES[@]}"; do
        echo "   $entry"
    done
fi
echo ""

# ============================================================================
# Deployment Summary
# ============================================================================
echo -e "${GREEN}╔══════════════════════════════════════════════════════════════╗${NC}"
echo -e "${GREEN}║   ✅ Deployment Complete!                                    ║${NC}"
echo -e "${GREEN}╚══════════════════════════════════════════════════════════════╝${NC}"
echo ""
echo -e "${BLUE}📊 Check deployment status:${NC}"
echo "   kubectl get pods -n ${NAMESPACE}"
echo "   kubectl get svc -n ${NAMESPACE}"
echo "   kubectl get ingress -n ${NAMESPACE}"
echo ""
echo -e "${BLUE}🌐 Access Services:${NC}"
echo "   Frontend:        http://frontend.local"
echo "   Authorization:   http://authorization.local"
echo "   Candidate:       http://candidate.local"
echo "   Interview:       http://interview.local"
echo "   Recruitment:     http://recruitment.local"
echo "   Vacancy:         http://vacancy.local"
echo "   Keycloak:        http://keycloak.ats.local"
echo "   Grafana:         http://grafana.local (admin/admin)"
echo "   Prometheus:      http://prometheus.local"
echo ""
echo -e "${BLUE}🔍 Useful Commands:${NC}"
echo "   View all pods:           kubectl get pods -n ${NAMESPACE}"
echo "   View service logs:       kubectl logs -f deployment/<service-name> -n ${NAMESPACE}"
echo "   Port forward Grafana:    kubectl port-forward -n ${NAMESPACE} svc/ats-observability-grafana 3000:80"
echo "   Port forward Prometheus: kubectl port-forward -n ${NAMESPACE} svc/ats-observability-kube-prometheus-prometheus 9090:9090"
echo ""
echo -e "${GREEN}🎉 All done!${NC}"
