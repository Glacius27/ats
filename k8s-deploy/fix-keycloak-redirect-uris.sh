#!/bin/bash

# Script to fix Keycloak redirect URIs for the frontend client
# This updates the ats-frontend client to include the Kubernetes redirect URIs

set -e

NAMESPACE="${1:-ats}"

# Colors for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

show_manual_instructions() {
    local KEYCLOAK_POD="$1"
    echo -e "${YELLOW}📖 Manual Update Instructions:${NC}"
    echo ""
    echo "1. Port forward Keycloak:"
    echo "   kubectl port-forward -n ${NAMESPACE} ${KEYCLOAK_POD} 8080:8080"
    echo ""
    echo "2. Open Keycloak Admin Console:"
    echo "   http://localhost:8080"
    echo "   Username: admin"
    echo "   Password: admin"
    echo ""
    echo "3. Navigate to:"
    echo "   Realm: ats → Clients → ats-frontend → Settings"
    echo ""
    echo "4. In 'Valid Redirect URIs', add:"
    echo "   http://frontend.local/*"
    echo "   http://frontend.local/"
    echo "   http://frontend.local"
    echo ""
    echo "5. Click 'Save'"
    echo ""
}

echo -e "${BLUE}🔧 Fixing Keycloak Redirect URIs${NC}"
echo ""

# Check if kubectl is installed
if ! command -v kubectl &> /dev/null; then
    echo -e "${RED}❌ kubectl is not installed.${NC}"
    exit 1
fi

# Check if namespace exists
if ! kubectl get namespace "$NAMESPACE" &> /dev/null; then
    echo -e "${RED}❌ Namespace '${NAMESPACE}' does not exist.${NC}"
    exit 1
fi

# Check if Keycloak pod is running
KEYCLOAK_POD=$(kubectl get pods -n "$NAMESPACE" -l app=keycloak -o jsonpath='{.items[0].metadata.name}' 2>/dev/null || echo "")

if [ -z "$KEYCLOAK_POD" ]; then
    echo -e "${RED}❌ Keycloak pod not found in namespace '${NAMESPACE}'.${NC}"
    echo "   Make sure Keycloak is deployed first."
    exit 1
fi

echo -e "${GREEN}✅ Found Keycloak pod: ${KEYCLOAK_POD}${NC}"
echo ""

# Wait for Keycloak to be ready
echo -e "${YELLOW}⏳ Waiting for Keycloak to be ready...${NC}"
for i in {1..30}; do
    if kubectl exec -n "$NAMESPACE" "$KEYCLOAK_POD" -- curl -s http://localhost:8080/health/ready &>/dev/null; then
        echo -e "${GREEN}✅ Keycloak is ready${NC}"
        break
    fi
    if [ $i -eq 30 ]; then
        echo -e "${YELLOW}⚠️  Keycloak may still be starting. Continuing anyway...${NC}"
    fi
    sleep 2
done

echo ""
echo -e "${YELLOW}🔑 Getting admin access token...${NC}"

# Get admin token
TOKEN_RESPONSE=$(kubectl exec -n "$NAMESPACE" "$KEYCLOAK_POD" -- \
    curl -s -X POST "http://localhost:8080/realms/master/protocol/openid-connect/token" \
    -H "Content-Type: application/x-www-form-urlencoded" \
    -d "username=admin" \
    -d "password=admin" \
    -d "grant_type=password" \
    -d "client_id=admin-cli" 2>/dev/null || echo "")

if [ -z "$TOKEN_RESPONSE" ]; then
    echo -e "${RED}❌ Failed to get admin token. Keycloak may not be ready yet.${NC}"
    echo ""
    show_manual_instructions "$KEYCLOAK_POD"
    exit 1
fi

ADMIN_TOKEN=$(echo "$TOKEN_RESPONSE" | grep -oP '"access_token":"\K[^"]+' || echo "")

if [ -z "$ADMIN_TOKEN" ]; then
    echo -e "${RED}❌ Could not extract access token from response.${NC}"
    echo ""
    show_manual_instructions "$KEYCLOAK_POD"
    exit 1
fi

echo -e "${GREEN}✅ Got admin token${NC}"
echo ""

# Get client ID
echo -e "${YELLOW}📋 Getting client ID for ats-frontend...${NC}"
CLIENT_RESPONSE=$(kubectl exec -n "$NAMESPACE" "$KEYCLOAK_POD" -- \
    curl -s -X GET "http://localhost:8080/admin/realms/ats/clients?clientId=ats-frontend" \
    -H "Authorization: Bearer ${ADMIN_TOKEN}" \
    -H "Content-Type: application/json" 2>/dev/null || echo "")

CLIENT_ID=$(echo "$CLIENT_RESPONSE" | grep -oP '"id":"\K[^"]+' | head -1 || echo "")

if [ -z "$CLIENT_ID" ]; then
    echo -e "${RED}❌ Could not find ats-frontend client.${NC}"
    echo "   Make sure the realm 'ats' exists and the client 'ats-frontend' is configured."
    show_manual_instructions "$KEYCLOAK_POD"
    exit 1
fi

echo -e "${GREEN}✅ Found client ID: ${CLIENT_ID}${NC}"
echo ""

# Update redirect URIs
echo -e "${YELLOW}🔄 Updating redirect URIs...${NC}"

REDIRECT_URIS='["http://localhost:3000/*","http://localhost:3000/","http://localhost:3000","http://localhost:5173/*","http://localhost:5173/","http://localhost:5173","http://frontend.local/*","http://frontend.local/","http://frontend.local"]'

UPDATE_RESPONSE=$(kubectl exec -n "$NAMESPACE" "$KEYCLOAK_POD" -- \
    curl -s -w "\n%{http_code}" -X PUT "http://localhost:8080/admin/realms/ats/clients/${CLIENT_ID}" \
    -H "Authorization: Bearer ${ADMIN_TOKEN}" \
    -H "Content-Type: application/json" \
    -d "{\"redirectUris\":${REDIRECT_URIS}}" 2>/dev/null || echo "")

HTTP_CODE=$(echo "$UPDATE_RESPONSE" | tail -1)

if [ "$HTTP_CODE" = "204" ] || [ "$HTTP_CODE" = "200" ]; then
    echo -e "${GREEN}✅ Redirect URIs updated successfully!${NC}"
    echo ""
    echo -e "${BLUE}📝 Updated redirect URIs:${NC}"
    echo "   - http://localhost:3000/*"
    echo "   - http://localhost:3000/"
    echo "   - http://localhost:3000"
    echo "   - http://localhost:5173/*"
    echo "   - http://localhost:5173/"
    echo "   - http://localhost:5173"
    echo "   - http://frontend.local/*"
    echo "   - http://frontend.local/"
    echo "   - http://frontend.local"
    echo ""
    echo -e "${GREEN}🎉 Keycloak configuration updated!${NC}"
    echo ""
    echo -e "${YELLOW}💡 If the frontend still shows the error:${NC}"
    echo "   1. Clear your browser cache"
    echo "   2. Restart the frontend pod: kubectl rollout restart deployment/ats-frontend -n ${NAMESPACE}"
else
    echo -e "${RED}❌ Failed to update redirect URIs. HTTP code: ${HTTP_CODE}${NC}"
    echo ""
    show_manual_instructions "$KEYCLOAK_POD"
    exit 1
fi
