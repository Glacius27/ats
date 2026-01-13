#!/bin/bash

# Deploy ATS microservices to Kubernetes
# Usage: ./deploy-services.sh [namespace]

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
NAMESPACE="${1:-ats}"

# Colors for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${BLUE}🚀 Deploying ATS microservices to Kubernetes${NC}"
echo -e "${BLUE}Namespace: ${NAMESPACE}${NC}"
echo ""

# Check if kubectl is installed
if ! command -v kubectl &> /dev/null; then
    echo "❌ kubectl is not installed. Please install kubectl first."
    exit 1
fi

# Check if helm is installed
if ! command -v helm &> /dev/null; then
    echo "❌ Helm is not installed. Please install Helm first."
    exit 1
fi

# Check if Kubernetes is running
if ! kubectl cluster-info &> /dev/null; then
    echo "❌ Kubernetes cluster is not accessible. Please ensure Docker Desktop Kubernetes is enabled."
    exit 1
fi

# Verify this is Docker Desktop Kubernetes (not KIND)
if kubectl get nodes -o name 2>/dev/null | grep -q "kind"; then
    echo "⚠️  WARNING: KIND cluster detected. This script is configured for Docker Desktop Kubernetes."
    echo "   Please switch to Docker Desktop Kubernetes or delete the KIND cluster:"
    echo "   kind delete cluster --name ats"
    read -p "   Continue anyway? (y/N) " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        exit 1
    fi
fi

# Create namespace
echo -e "${YELLOW}📦 Creating namespace: ${NAMESPACE}${NC}"
kubectl create namespace "$NAMESPACE" --dry-run=client -o yaml | kubectl apply -f -

# Build Docker images first (if not already built)
echo ""
echo -e "${YELLOW}🐳 Building Docker images...${NC}"
cd "$PROJECT_ROOT"

# Check if images exist, if not build them
SERVICES=("ats-authorization-service" "ats-candidate-service" "ats-interview-service" "ats-recruitment-service" "ats-vacancy-service")

for service in "${SERVICES[@]}"; do
    image_name="ats-${service}:latest"
    if docker image inspect "$image_name" &>/dev/null; then
        echo "Image ${image_name} already exists, skipping build"
    else
        echo "Building ${service}..."
        docker build -f "${service}/Dockerfile" -t "$image_name" .
    fi
done

# Verify images exist (Docker Desktop Kubernetes shares Docker daemon)
echo ""
echo -e "${YELLOW}📥 Verifying Docker images...${NC}"
for service in "${SERVICES[@]}"; do
    image_name="ats-${service}:latest"
    # Use docker image inspect which is more reliable than grep
    if docker image inspect "$image_name" &>/dev/null; then
        echo "✅ Image found: ${image_name}"
    else
        echo "⚠️  Image not found: ${image_name} - please build it first"
        echo "   Run: cd .. && ./build-docker-images.sh"
        exit 1
    fi
done
echo ""
echo "ℹ️  Docker Desktop Kubernetes uses the same Docker daemon, so local images are automatically available"

# Deploy services using Helm
echo ""
echo -e "${YELLOW}📦 Deploying services with Helm...${NC}"

cd "$PROJECT_ROOT/helm/ats-services"

SERVICES=("ats-authorization-service" "ats-candidate-service" "ats-interview-service" "ats-recruitment-service" "ats-vacancy-service")

for service in "${SERVICES[@]}"; do
    echo ""
    echo -e "${BLUE}Deploying ${service}...${NC}"
    
    # First deploy without ingress to avoid webhook issues
    # Docker Desktop shares Docker daemon, so IfNotPresent should work
    helm upgrade --install "${service}" "./${service}" \
        --namespace "$NAMESPACE" \
        --set image.repository="ats-${service}" \
        --set image.tag="latest" \
        --set image.pullPolicy="IfNotPresent" \
        --set ingress.enabled=false \
        --wait --timeout=5m || echo "⚠️  ${service} deployment may still be in progress"
done

# Wait a bit for services to be ready
echo ""
echo -e "${YELLOW}⏳ Waiting for services to stabilize...${NC}"
sleep 10

# Now enable ingress for all services
echo ""
echo -e "${YELLOW}🌐 Enabling ingress for all services...${NC}"
for service in "${SERVICES[@]}"; do
    echo -e "${BLUE}Enabling ingress for ${service}...${NC}"
    helm upgrade "${service}" "./${service}" \
        --namespace "$NAMESPACE" \
        --set image.repository="ats-${service}" \
        --set image.tag="latest" \
        --set image.pullPolicy="IfNotPresent" \
        --set ingress.enabled=true \
        --wait --timeout=2m || echo "⚠️  ${service} ingress may still be in progress"
done

echo ""
echo -e "${GREEN}✅ All services deployed!${NC}"
echo ""
echo "📊 Check deployment status:"
echo "   kubectl get pods -n ${NAMESPACE}"
echo "   kubectl get svc -n ${NAMESPACE}"
echo "   kubectl get ingress -n ${NAMESPACE}"
echo ""
echo "📝 Don't forget to add these to your /etc/hosts:"
echo "   127.0.0.1 authorization.local"
echo "   127.0.0.1 candidate.local"
echo "   127.0.0.1 interview.local"
echo "   127.0.0.1 recruitment.local"
echo "   127.0.0.1 vacancy.local"
