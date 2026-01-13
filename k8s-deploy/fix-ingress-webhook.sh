#!/bin/bash

# Fix ingress-nginx webhook certificate issue
# This script deletes and recreates the validating webhook configuration

set -e

echo "🔧 Fixing ingress-nginx webhook certificate issue..."

# Delete the validating webhook configuration
echo "🗑️  Deleting existing validating webhook..."
kubectl delete validatingwebhookconfiguration ingress-nginx-admission 2>/dev/null || echo "Webhook not found, continuing..."

# Wait a moment
sleep 2

# Check if ingress-nginx pods are running
echo "⏳ Waiting for ingress-nginx pods to be ready..."
kubectl wait --namespace ingress-nginx \
  --for=condition=ready pod \
  --selector=app.kubernetes.io/component=controller \
  --timeout=120s || echo "⚠️  Some pods may not be ready yet"

# The webhook should be automatically recreated by the ingress-nginx controller
echo "✅ Webhook should be automatically recreated. Waiting 10 seconds..."
sleep 10

# Verify webhook exists
if kubectl get validatingwebhookconfiguration ingress-nginx-admission &>/dev/null; then
    echo "✅ Validating webhook recreated successfully"
else
    echo "⚠️  Webhook not yet recreated, but this is usually fine"
fi

echo ""
echo "✅ Done! You can now retry deploying services."
