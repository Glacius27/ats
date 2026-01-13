#!/bin/bash

# Update /etc/hosts file with ATS service domains
# This script adds the necessary entries to /etc/hosts

set -e

HOSTS_FILE="/etc/hosts"
HOSTS_ENTRIES=(
    "127.0.0.1 authorization.local"
    "127.0.0.1 candidate.local"
    "127.0.0.1 interview.local"
    "127.0.0.1 recruitment.local"
    "127.0.0.1 vacancy.local"
)

echo "🔧 Updating /etc/hosts file..."

# Check if running as root (required for /etc/hosts)
if [ "$EUID" -ne 0 ]; then 
    echo "⚠️  This script needs sudo privileges to modify /etc/hosts"
    echo "Please run: sudo $0"
    exit 1
fi

# Backup hosts file
cp "$HOSTS_FILE" "${HOSTS_FILE}.backup.$(date +%Y%m%d_%H%M%S)"
echo "✅ Backup created: ${HOSTS_FILE}.backup.$(date +%Y%m%d_%H%M%S)"

# Remove existing ATS entries
echo "🧹 Removing existing ATS entries..."
sed -i.bak '/# ATS Services/,/# End ATS Services/d' "$HOSTS_FILE" 2>/dev/null || true

# Add new entries
echo "➕ Adding ATS service entries..."
echo "" >> "$HOSTS_FILE"
echo "# ATS Services" >> "$HOSTS_FILE"
for entry in "${HOSTS_ENTRIES[@]}"; do
    if ! grep -q "$entry" "$HOSTS_FILE"; then
        echo "$entry" >> "$HOSTS_FILE"
        echo "  Added: $entry"
    else
        echo "  Already exists: $entry"
    fi
done
echo "# End ATS Services" >> "$HOSTS_FILE"

echo ""
echo "✅ /etc/hosts updated successfully!"
echo ""
echo "📝 Added entries:"
for entry in "${HOSTS_ENTRIES[@]}"; do
    echo "   $entry"
done
