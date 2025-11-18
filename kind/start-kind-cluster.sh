#!/bin/bash

echo "=== Starting kind cluster containers ==="

docker ps -a --filter "name=ats-" --format "{{.ID}}" | xargs -r docker start

echo "=== kind cluster started ==="