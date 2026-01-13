#!/bin/bash

# Build script for ATS microservices Docker images
# Usage: ./build-docker-images.sh [service-name] [tag]
# If no service-name is provided, builds all services

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

# Default values
TAG="${2:-latest}"
SERVICE_NAME="${1:-all}"

# Colors for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

build_service() {
    local service=$1
    local tag=$2
    local dockerfile_path="${service}/Dockerfile"
    
    if [ ! -f "$dockerfile_path" ]; then
        echo "❌ Dockerfile not found: $dockerfile_path"
        return 1
    fi
    
    echo -e "${BLUE}Building ${service}...${NC}"
    docker build -f "$dockerfile_path" -t "ats-${service}:${tag}" .
    
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✅ Successfully built ats-${service}:${tag}${NC}"
    else
        echo -e "❌ Failed to build ${service}"
        return 1
    fi
}

if [ "$SERVICE_NAME" = "all" ]; then
    echo "Building all ATS microservices..."
    echo ""
    
    build_service "ats-authorization-service" "$TAG"
    build_service "ats-candidate-service" "$TAG"
    build_service "ats-interview-service" "$TAG"
    build_service "ats-recruitment-service" "$TAG"
    build_service "ats-vacancy-service" "$TAG"
    
    echo ""
    echo -e "${GREEN}All services built successfully!${NC}"
else
    build_service "$SERVICE_NAME" "$TAG"
fi
