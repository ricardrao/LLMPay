#!/bin/bash

# LLMPay Quick Start Script for Linux/Mac

echo ""
echo "============================================"
echo "   LLMPay Project Setup"
echo "============================================"
echo ""

# Check .NET SDK
echo "[1/5] Checking .NET SDK..."
if ! command -v dotnet &> /dev/null; then
    echo "Error: .NET SDK not found!"
    echo "Please install .NET 8.0 SDK from https://dotnet.microsoft.com/download"
    exit 1
fi
dotnet --version

# Restore packages
echo ""
echo "[2/5] Restoring NuGet packages..."
dotnet restore

# Build solution
echo ""
echo "[3/5] Building solution..."
dotnet build

# Create database
echo ""
echo "[4/5] Creating database..."
echo "Note: Make sure PostgreSQL is running on localhost:5432"
echo "      Default: postgres/postgres"
sleep 2

cd LLMPay.Api
dotnet ef migrations add InitialCreate --project ../LLMPay.Infrastructure
dotnet ef database update --project ../LLMPay.Infrastructure
cd ..

echo ""
echo "[5/5] Setup complete!"
echo ""
echo "============================================"
echo "   To start the API:"
echo "   cd LLMPay.Api && dotnet run"
echo "============================================"
echo ""
echo "API will be available at:"
echo "   https://localhost:5001"
echo "   http://localhost:5000"
echo ""
echo "Swagger UI: https://localhost:5001/swagger"
echo ""

# Optional: Start PostgreSQL if using Docker
read -p "Do you want to start PostgreSQL using Docker? (y/n) " -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    echo "Starting PostgreSQL container..."
    docker-compose up -d
    echo "PostgreSQL is starting... (wait a few seconds for it to be ready)"
fi
