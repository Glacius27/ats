#!/bin/bash

set -e

NAMESPACE="ats"

echo "=== Installing infrastructure ==="
helm install infra ./helm/ats-infra -n "$NAMESPACE" --create-namespace

echo "=== Waiting 20 seconds for infra to settle... ==="
sleep 20

echo "=== Installing Authorization Service ==="
helm install authorization-service ./helm/ats-services/ats-authorization-service -n "$NAMESPACE"

echo "=== All components installed ==="