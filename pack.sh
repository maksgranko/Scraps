#!/bin/bash
set -e

cd "$(dirname "$0")"

NUGET_FOLDER="$(pwd)/NuGet"
mkdir -p "$NUGET_FOLDER"

echo "=========================================="
echo "  Scraps: Build + Pack all projects"
echo "=========================================="
echo ""

echo "[1/3] Building solution..."
dotnet build Scraps.sln -c Release

echo ""
echo "[2/3] Packing all projects into NuGet/..."
dotnet pack Scraps.sln -c Release --no-build -o "$NUGET_FOLDER"

echo ""
echo "[3/3] Created packages:"
ls -1 "$NUGET_FOLDER"/*.nupkg 2>/dev/null || echo "No .nupkg files found"

echo ""
echo "=========================================="
echo "  Done. Packages are in: $NUGET_FOLDER"
echo "=========================================="
