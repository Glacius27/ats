#!/bin/bash

set -e

NAMESPACE="ats"

echo "=== Removing all helm releases in namespace '$NAMESPACE' ==="

# Delete service charts (если станет больше — добавим)
helm uninstall authorization-service -n "$NAMESPACE" || true

# Delete infra chart
helm uninstall infra -n "$NAMESPACE" || true

echo "=== All helm releases removed ==="