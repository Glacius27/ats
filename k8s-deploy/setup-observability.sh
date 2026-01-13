#!/bin/bash

# Setup Prometheus and Grafana for ATS microservices
# This script installs the observability stack and configures it

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
NAMESPACE="${1:-ats}"

# Colors for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${BLUE}📊 Setting up Observability Stack (Prometheus + Grafana)${NC}"
echo ""

# Check if helm is installed
if ! command -v helm &> /dev/null; then
    echo "❌ Helm is not installed. Please install Helm first."
    exit 1
fi

# Check if kubectl is installed
if ! command -v kubectl &> /dev/null; then
    echo "❌ kubectl is not installed. Please install kubectl first."
    exit 1
fi

# Check if Kubernetes is running
if ! kubectl cluster-info &> /dev/null; then
    echo "❌ Kubernetes cluster is not accessible."
    exit 1
fi

# Create namespace if it doesn't exist
kubectl create namespace "$NAMESPACE" --dry-run=client -o yaml | kubectl apply -f -

# Add Prometheus Helm repository
echo -e "${YELLOW}📦 Adding Prometheus Helm repositories...${NC}"
helm repo add prometheus-community https://prometheus-community.github.io/helm-charts
helm repo add grafana https://grafana.github.io/helm-charts
helm repo update

# Install observability stack
echo ""
echo -e "${YELLOW}🚀 Installing observability stack...${NC}"
cd "$PROJECT_ROOT/helm/ats-observability"

helm upgrade --install ats-observability . \
    --namespace "$NAMESPACE" \
    --wait --timeout=10m || echo "⚠️  Installation may still be in progress"

echo ""
echo -e "${GREEN}✅ Observability stack installed!${NC}"
echo ""
echo "📊 Access Grafana:"
echo "   URL: http://grafana.local"
echo "   Username: admin"
echo "   Password: admin"
echo ""
echo "📈 Access Prometheus:"
echo "   URL: http://prometheus.local"
echo ""
echo "💡 Don't forget to add to /etc/hosts:"
echo "   127.0.0.1 grafana.local"
echo "   127.0.0.1 prometheus.local"
echo ""
echo "🔍 Check ServiceMonitors:"
echo "   kubectl get servicemonitor -n $NAMESPACE"
echo ""
echo "📊 Check Prometheus targets:"
echo "   kubectl port-forward -n $NAMESPACE svc/ats-observability-kube-prometheus-prometheus 9090:9090"
echo "   Then open http://localhost:9090/targets"
