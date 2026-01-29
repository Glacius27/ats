#!/bin/bash

# Script to stop and remove all ATS deployments from local K8s cluster
# Usage: ./stop-k8s.sh [namespace] [--remove-namespace] [--remove-ingress]

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
NAMESPACE="${1:-ats}"

# Parse arguments
REMOVE_NAMESPACE=false
REMOVE_INGRESS=false

for arg in "$@"; do
    case $arg in
        --remove-namespace)
            REMOVE_NAMESPACE=true
            shift
            ;;
        --remove-ingress)
            REMOVE_INGRESS=true
            shift
            ;;
    esac
done

# Colors for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

echo -e "${BLUE}╔══════════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║   ATS Kubernetes Cleanup Script                            ║${NC}"
echo -e "${BLUE}╚══════════════════════════════════════════════════════════════╝${NC}"
echo ""
echo -e "${BLUE}Namespace: ${NAMESPACE}${NC}"
echo ""

# Check if kubectl is installed
if ! command -v kubectl &> /dev/null; then
    echo -e "${RED}❌ kubectl is not installed.${NC}"
    exit 1
fi

# Check if helm is installed
if ! command -v helm &> /dev/null; then
    echo -e "${RED}❌ Helm is not installed.${NC}"
    exit 1
fi

# Check if namespace exists
if ! kubectl get namespace "$NAMESPACE" &> /dev/null; then
    echo -e "${YELLOW}⚠️  Namespace '${NAMESPACE}' does not exist. Nothing to clean up.${NC}"
    exit 0
fi

# ============================================================================
# Uninstall Helm Releases
# ============================================================================
echo -e "${YELLOW}🗑️  Step 1: Uninstalling Helm releases...${NC}"

# Get all helm releases in the namespace
RELEASES=$(helm list -n "$NAMESPACE" -q 2>/dev/null || echo "")

if [ -z "$RELEASES" ]; then
    echo "  No Helm releases found in namespace '${NAMESPACE}'"
else
    for release in $RELEASES; do
        echo "  Uninstalling ${release}..."
        helm uninstall "$release" -n "$NAMESPACE" 2>/dev/null || echo "    ⚠️  Failed to uninstall ${release} (may not exist)"
    done
fi

echo -e "${GREEN}✅ Helm releases uninstalled${NC}"
echo ""

# ============================================================================
# Wait for Resources to Terminate
# ============================================================================
echo -e "${YELLOW}⏳ Step 2: Waiting for resources to terminate...${NC}"

# Wait for pods to terminate
TIMEOUT=120
ELAPSED=0
while [ $ELAPSED -lt $TIMEOUT ]; do
    POD_COUNT=$(kubectl get pods -n "$NAMESPACE" --no-headers 2>/dev/null | wc -l | tr -d ' ')
    if [ "$POD_COUNT" -eq 0 ]; then
        break
    fi
    if [ $((ELAPSED % 10)) -eq 0 ]; then
        echo "  Waiting for pods to terminate... ($ELAPSED/$TIMEOUT seconds)"
    fi
    sleep 2
    ELAPSED=$((ELAPSED + 2))
done

if [ "$POD_COUNT" -gt 0 ]; then
    echo -e "${YELLOW}⚠️  Some pods are still running. Forcing deletion...${NC}"
    kubectl delete pods --all -n "$NAMESPACE" --force --grace-period=0 2>/dev/null || true
fi

echo -e "${GREEN}✅ Resources terminated${NC}"
echo ""

# ============================================================================
# Remove Ingress Controller (Optional)
# ============================================================================
if [ "$REMOVE_INGRESS" = true ]; then
    echo -e "${YELLOW}🌐 Step 3: Removing Ingress Controller...${NC}"
    
    if kubectl get namespace ingress-nginx &> /dev/null; then
        helm uninstall ingress-nginx -n ingress-nginx 2>/dev/null || echo "    ⚠️  Failed to uninstall ingress-nginx"
        kubectl delete namespace ingress-nginx --timeout=60s 2>/dev/null || echo "    ⚠️  Failed to delete ingress-nginx namespace"
        echo -e "${GREEN}✅ Ingress controller removed${NC}"
    else
        echo "  Ingress controller namespace does not exist"
    fi
    echo ""
fi

# ============================================================================
# Remove Namespace (Optional)
# ============================================================================
if [ "$REMOVE_NAMESPACE" = true ]; then
    echo -e "${YELLOW}📦 Step 4: Removing namespace...${NC}"
    
    # Delete all remaining resources in namespace
    kubectl delete all --all -n "$NAMESPACE" --timeout=60s 2>/dev/null || true
    kubectl delete configmap,secret --all -n "$NAMESPACE" --timeout=60s 2>/dev/null || true
    kubectl delete ingress --all -n "$NAMESPACE" --timeout=60s 2>/dev/null || true
    kubectl delete pvc --all -n "$NAMESPACE" --timeout=60s 2>/dev/null || true
    
    # Delete namespace
    kubectl delete namespace "$NAMESPACE" --timeout=120s 2>/dev/null || {
        echo -e "${YELLOW}⚠️  Namespace deletion may take some time. Run 'kubectl get namespace ${NAMESPACE}' to check status.${NC}"
    }
    
    echo -e "${GREEN}✅ Namespace removed${NC}"
    echo ""
fi

# ============================================================================
# Cleanup Summary
# ============================================================================
echo -e "${GREEN}╔══════════════════════════════════════════════════════════════╗${NC}"
echo -e "${GREEN}║   ✅ Cleanup Complete!                                      ║${NC}"
echo -e "${GREEN}╚══════════════════════════════════════════════════════════════╝${NC}"
echo ""

if [ "$REMOVE_NAMESPACE" = false ]; then
    echo -e "${BLUE}ℹ️  Namespace '${NAMESPACE}' still exists.${NC}"
    echo "   To remove it, run: kubectl delete namespace ${NAMESPACE}"
    echo ""
fi

if [ "$REMOVE_INGRESS" = false ]; then
    echo -e "${BLUE}ℹ️  Ingress controller still exists.${NC}"
    echo "   To remove it, run: ./stop-k8s.sh ${NAMESPACE} --remove-ingress"
    echo ""
fi

echo -e "${BLUE}📝 Optional cleanup:${NC}"
echo "   Remove Docker images:    docker rmi \$(docker images 'ats-*' -q)"
echo "   Remove unused volumes:   docker volume prune"
echo "   Remove unused networks:   docker network prune"
echo ""
echo -e "${GREEN}🎉 Done!${NC}"
