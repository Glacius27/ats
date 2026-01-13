#!/bin/bash

# Import Grafana dashboard for ATS Microservices
# This script imports the dashboard JSON into Grafana

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
DASHBOARD_FILE="$PROJECT_ROOT/observability/grafana/ats-microservices-dashboard.json"
NAMESPACE="${1:-ats}"

# Colors for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${BLUE}📊 Importing ATS Microservices Dashboard to Grafana${NC}"
echo ""

# Check if dashboard file exists
if [ ! -f "$DASHBOARD_FILE" ]; then
    echo "❌ Dashboard file not found: $DASHBOARD_FILE"
    exit 1
fi

# Get Grafana service
GRAFANA_SVC=$(kubectl get svc -n "$NAMESPACE" -l app.kubernetes.io/name=grafana -o jsonpath='{.items[0].metadata.name}' 2>/dev/null || echo "")

if [ -z "$GRAFANA_SVC" ]; then
    echo "❌ Grafana service not found in namespace $NAMESPACE"
    exit 1
fi

echo -e "${YELLOW}📦 Setting up port-forward to Grafana...${NC}"

# Start port-forward in background
kubectl port-forward -n "$NAMESPACE" svc/"$GRAFANA_SVC" 3000:80 > /dev/null 2>&1 &
PORT_FORWARD_PID=$!

# Wait for port-forward to be ready
sleep 3

# Check if port-forward is working
if ! curl -s http://localhost:3000/api/health > /dev/null 2>&1; then
    echo "❌ Failed to connect to Grafana"
    kill $PORT_FORWARD_PID 2>/dev/null || true
    exit 1
fi

echo -e "${YELLOW}🔐 Getting Grafana admin credentials...${NC}"

# Get admin password from secret or use default
GRAFANA_SECRET=$(kubectl get secret -n "$NAMESPACE" -l app.kubernetes.io/name=grafana -o jsonpath='{.items[0].metadata.name}' 2>/dev/null || echo "")
if [ -n "$GRAFANA_SECRET" ]; then
    GRAFANA_PASSWORD=$(kubectl get secret -n "$NAMESPACE" "$GRAFANA_SECRET" -o jsonpath='{.data.admin-password}' 2>/dev/null | base64 -d || echo "admin")
else
    GRAFANA_PASSWORD="admin"
fi

echo -e "${YELLOW}📥 Importing dashboard...${NC}"

# Read dashboard JSON and wrap it in the proper format for Grafana API
DASHBOARD_JSON=$(cat "$DASHBOARD_FILE")
# Grafana API expects: {dashboard: {...}, overwrite: true}
IMPORT_PAYLOAD=$(echo "$DASHBOARD_JSON" | jq '{dashboard: .dashboard, overwrite: true}')

# Import dashboard using Grafana API
TEMP_FILE=$(mktemp)
curl -s -w "\n%{http_code}" -X POST \
    -H "Content-Type: application/json" \
    -u "admin:$GRAFANA_PASSWORD" \
    -d "$IMPORT_PAYLOAD" \
    http://localhost:3000/api/dashboards/db > "$TEMP_FILE" 2>&1

HTTP_CODE=$(tail -1 "$TEMP_FILE")
RESPONSE_BODY=$(head -n -1 "$TEMP_FILE" 2>/dev/null || sed '$d' "$TEMP_FILE")
rm -f "$TEMP_FILE"

# Check if import was successful
if [ "$HTTP_CODE" = "200" ] || echo "$RESPONSE_BODY" | grep -q '"status":"success"'; then
    DASHBOARD_URL=$(echo "$RESPONSE_BODY" | grep -o '"url":"[^"]*"' | cut -d'"' -f4 || echo "/d/$(echo "$RESPONSE_BODY" | grep -o '"uid":"[^"]*"' | cut -d'"' -f4)")
    echo ""
    echo -e "${GREEN}✅ Dashboard imported successfully!${NC}"
    echo ""
    echo "📊 Dashboard URL: http://localhost:3000$DASHBOARD_URL"
    echo ""
    echo "💡 To access via ingress:"
    echo "   1. Add to /etc/hosts: 127.0.0.1 grafana.local"
    echo "   2. Open: http://grafana.local"
    echo "   3. Login: admin / $GRAFANA_PASSWORD"
    echo ""
    echo "🔄 Port-forward is running (PID: $PORT_FORWARD_PID)"
    echo "   Press Ctrl+C to stop port-forward"
else
    echo "❌ Failed to import dashboard"
    echo "HTTP Code: $HTTP_CODE"
    echo "Response: $RESPONSE_BODY"
    kill $PORT_FORWARD_PID 2>/dev/null || true
    exit 1
fi

# Keep port-forward running
trap "kill $PORT_FORWARD_PID 2>/dev/null || true" EXIT
wait $PORT_FORWARD_PID
