#!/bin/bash
set -e

cd "$(dirname "$0")"

NUGET_FOLDER="$(pwd)/NuGet"
mkdir -p "$NUGET_FOLDER"

echo "=========================================="
echo "  Scraps: Build + Pack all projects"
echo "=========================================="
echo ""

echo "[1/4] Cleaning previous build..."
dotnet clean Scraps.sln -c Release

echo ""
echo "[2/4] Building solution..."
dotnet build Scraps.sln -c Release --no-restore

echo ""
echo "[3/4] Packing all projects into NuGet/..."
dotnet pack Scraps.sln -c Release --no-build --no-restore -o "$NUGET_FOLDER"

echo ""
echo "[4/4] Created packages:"
ls -1 "$NUGET_FOLDER"/*.nupkg 2>/dev/null || echo "No .nupkg files found"

echo ""
echo "=========================================="
echo "  Done. Packages are in: $NUGET_FOLDER"
echo "=========================================="
