#!/bin/bash
set -e

cd "$(dirname "$0")"

NUGET_FOLDER="$(pwd)/NuGet"

if [ ! -d "$NUGET_FOLDER" ] || [ -z "$(ls -A "$NUGET_FOLDER"/*.nupkg 2>/dev/null)" ]; then
    echo "No packages found in $NUGET_FOLDER"
    echo "Run ./pack.sh or ./pack.bat first."
    exit 1
fi

echo "=========================================="
echo "  Scraps: Publish to NuGet.org"
echo "=========================================="
echo ""

read -sp "Enter NuGet.org API Key: " APIKEY
echo ""

if [ -z "$APIKEY" ]; then
    echo "API Key is required."
    exit 1
fi

echo "Publishing packages..."
for pkg in "$NUGET_FOLDER"/*.nupkg; do
    echo "  - Publishing $(basename "$pkg")..."
    dotnet nuget push "$pkg" -k "$APIKEY" -s https://api.nuget.org/v3/index.json --skip-duplicate
done

echo ""
echo "=========================================="
echo "  Done."
echo "=========================================="
