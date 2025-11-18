#!/bin/bash

echo "=== Stopping kind cluster containers ==="

docker ps --filter "name=ats-" --format "{{.ID}}" | xargs -r docker stop

echo "=== kind cluster stopped ==="