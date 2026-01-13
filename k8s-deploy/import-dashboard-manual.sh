#!/bin/bash

# Simple script to help with manual dashboard import
# This sets up port-forward and provides instructions

set -e

NAMESPACE="${1:-ats}"

echo "📊 Grafana Dashboard Import Helper"
echo ""
echo "This script will set up port-forward to Grafana."
echo "Then you can manually import the dashboard via the UI."
echo ""

# Get Grafana service
GRAFANA_SVC=$(kubectl get svc -n "$NAMESPACE" -l app.kubernetes.io/name=grafana -o jsonpath='{.items[0].metadata.name}' 2>/dev/null || echo "")

if [ -z "$GRAFANA_SVC" ]; then
    echo "❌ Grafana service not found in namespace $NAMESPACE"
    exit 1
fi

echo "🚀 Starting port-forward to Grafana..."
echo "   Access Grafana at: http://localhost:3000"
echo "   Username: admin"
echo "   Password: admin"
echo ""
echo "📥 To import dashboard:"
echo "   1. Go to http://localhost:3000"
echo "   2. Login with admin/admin"
echo "   3. Go to Dashboards → Import"
echo "   4. Upload: observability/grafana/ats-microservices-dashboard.json"
echo "   5. Select Prometheus as datasource"
echo "   6. Click Import"
echo ""
echo "Press Ctrl+C to stop port-forward"
echo ""

kubectl port-forward -n "$NAMESPACE" svc/"$GRAFANA_SVC" 3000:80
