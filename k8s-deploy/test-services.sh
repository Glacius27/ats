#!/bin/bash

# Test ATS microservices via HTTP requests
# This script makes sample HTTP requests to all deployed services

set -e

# Colors for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${BLUE}🧪 Testing ATS Microservices${NC}"
echo ""

# Check if curl is installed
if ! command -v curl &> /dev/null; then
    echo -e "${RED}❌ curl is not installed. Please install curl first.${NC}"
    exit 1
fi

# Function to test an endpoint
test_endpoint() {
    local name=$1
    local url=$2
    local method=${3:-GET}
    
    echo -e "${YELLOW}Testing ${name}...${NC}"
    echo "  ${method} ${url}"
    
    if [ "$method" = "GET" ]; then
        response=$(curl -s -w "\n%{http_code}" -o /tmp/response_body.txt "$url" || echo "000")
    else
        response=$(curl -s -w "\n%{http_code}" -o /tmp/response_body.txt -X "$method" "$url" || echo "000")
    fi
    
    http_code=$(echo "$response" | tail -n1)
    body=$(cat /tmp/response_body.txt 2>/dev/null || echo "")
    
    if [ "$http_code" = "200" ] || [ "$http_code" = "201" ] || [ "$http_code" = "404" ]; then
        echo -e "  ${GREEN}✅ Status: ${http_code}${NC}"
        if [ -n "$body" ] && [ ${#body} -lt 200 ]; then
            echo "  Response: $body"
        fi
    else
        echo -e "  ${RED}❌ Status: ${http_code}${NC}"
        if [ -n "$body" ]; then
            echo "  Response: $body"
        fi
    fi
    echo ""
}

# Test Authorization Service
echo -e "${BLUE}=== Authorization Service ===${NC}"
test_endpoint "Health Check" "http://authorization.local/health"
test_endpoint "Swagger UI" "http://authorization.local/swagger/index.html"

# Test Candidate Service
echo -e "${BLUE}=== Candidate Service ===${NC}"
test_endpoint "Health Check" "http://candidate.local/health"
test_endpoint "Swagger UI" "http://candidate.local/swagger/index.html"
test_endpoint "Get Candidates" "http://candidate.local/api/candidates"

# Test Interview Service
echo -e "${BLUE}=== Interview Service ===${NC}"
test_endpoint "Health Check" "http://interview.local/health"
test_endpoint "Swagger UI" "http://interview.local/swagger/index.html"

# Test Recruitment Service
echo -e "${BLUE}=== Recruitment Service ===${NC}"
test_endpoint "Health Check" "http://recruitment.local/health"
test_endpoint "Swagger UI" "http://recruitment.local/swagger/index.html"

# Test Vacancy Service
echo -e "${BLUE}=== Vacancy Service ===${NC}"
test_endpoint "Health Check" "http://vacancy.local/health"
test_endpoint "Swagger UI" "http://vacancy.local/swagger/index.html"
test_endpoint "Get Vacancies" "http://vacancy.local/api/vacancies"

echo -e "${GREEN}✅ Testing complete!${NC}"
echo ""
echo "💡 Tip: If you see connection errors, make sure:"
echo "   1. Services are deployed: kubectl get pods -n ats"
echo "   2. Ingress is installed: kubectl get pods -n ingress-nginx"
echo "   3. Hosts are configured: cat /etc/hosts | grep local"
echo "   4. Services are ready: kubectl get ingress -n ats"
