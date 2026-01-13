#!/bin/bash

# Install NGINX Ingress Controller for Docker Desktop Kubernetes
# This script installs the ingress controller using Helm

set -e

echo "🔧 Installing NGINX Ingress Controller for Docker Desktop..."

# Check if helm is installed
if ! command -v helm &> /dev/null; then
    echo "❌ Helm is not installed. Please install Helm first:"
    echo "   macOS: brew install helm"
    echo "   Linux: https://helm.sh/docs/intro/install/"
    exit 1
fi

# Check if kubectl is installed
if ! command -v kubectl &> /dev/null; then
    echo "❌ kubectl is not installed. Please install kubectl first."
    exit 1
fi

# Check if Kubernetes is running
if ! kubectl cluster-info &> /dev/null; then
    echo "❌ Kubernetes cluster is not accessible. Please ensure Docker Desktop Kubernetes is enabled."
    exit 1
fi

# Add ingress-nginx Helm repository
echo "📦 Adding ingress-nginx Helm repository..."
helm repo add ingress-nginx https://kubernetes.github.io/ingress-nginx
helm repo update

# Install ingress-nginx
echo "🚀 Installing ingress-nginx..."
helm upgrade --install ingress-nginx ingress-nginx/ingress-nginx \
  --namespace ingress-nginx \
  --create-namespace \
  --set controller.service.type=LoadBalancer \
  --wait

echo ""
echo "✅ Ingress controller installed successfully!"
echo ""
echo "📝 To access services, add these entries to your /etc/hosts file:"
echo "   127.0.0.1 authorization.local"
echo "   127.0.0.1 candidate.local"
echo "   127.0.0.1 interview.local"
echo "   127.0.0.1 recruitment.local"
echo "   127.0.0.1 vacancy.local"
echo ""
echo "🔍 Check ingress controller status:"
echo "   kubectl get pods -n ingress-nginx"
echo "   kubectl get svc -n ingress-nginx"
