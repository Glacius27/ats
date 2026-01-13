#!/bin/bash

# Script to switch from KIND to Docker Desktop Kubernetes

set -e

echo "🔄 Switching from KIND to Docker Desktop Kubernetes..."
echo ""

# Check if KIND cluster exists
if kubectl get nodes 2>/dev/null | grep -q "ats-control-plane\|ats-worker"; then
    echo "⚠️  KIND cluster detected!"
    echo ""
    echo "This will:"
    echo "  1. Delete the KIND cluster"
    echo "  2. Verify Docker Desktop Kubernetes is enabled"
    echo ""
    read -p "Continue? (y/N) " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        echo "Cancelled."
        exit 0
    fi
    
    echo ""
    echo "🗑️  Deleting KIND cluster..."
    kind delete cluster --name ats 2>/dev/null || {
        echo "⚠️  Could not delete KIND cluster. Please delete manually:"
        echo "   kind delete cluster --name ats"
    }
    
    echo ""
    echo "⏳ Waiting 5 seconds for cleanup..."
    sleep 5
else
    echo "✅ No KIND cluster detected"
fi

# Check if Docker Desktop Kubernetes is available
echo ""
echo "🔍 Checking Docker Desktop Kubernetes..."

if ! kubectl cluster-info &>/dev/null; then
    echo ""
    echo "❌ Kubernetes is not accessible."
    echo ""
    echo "Please enable Docker Desktop Kubernetes:"
    echo "  1. Open Docker Desktop"
    echo "  2. Go to Settings → Kubernetes"
    echo "  3. Check 'Enable Kubernetes'"
    echo "  4. Click 'Apply & Restart'"
    echo "  5. Wait for Kubernetes to start"
    echo ""
    exit 1
fi

# Verify it's Docker Desktop
if kubectl get nodes 2>/dev/null | grep -q "docker-desktop"; then
    echo "✅ Docker Desktop Kubernetes is running!"
elif kubectl get nodes 2>/dev/null | grep -q "ats-control-plane"; then
    echo "❌ KIND cluster is still active. Please delete it first:"
    echo "   kind delete cluster --name ats"
    exit 1
else
    echo "⚠️  Unknown Kubernetes cluster detected"
    kubectl get nodes
fi

echo ""
echo "✅ Ready to use Docker Desktop Kubernetes!"
echo ""
echo "Next steps:"
echo "  1. Install ingress: ./install-ingress.sh"
echo "  2. Update hosts: sudo ./update-hosts.sh"
echo "  3. Build images: cd .. && ./build-docker-images.sh"
echo "  4. Deploy services: ./deploy-services.sh"
