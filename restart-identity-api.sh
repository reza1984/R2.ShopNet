#!/bin/bash

echo "🛑 Stopping Identity API..."
pkill -f "R2.ShopNet.Identity.API"

echo "⏳ Waiting for process to stop..."
sleep 2

echo "🔨 Building Identity API..."
cd /Volumes/Secure/Projects/R2.ShopNet/src/Services/Identity/R2.ShopNet.Identity.API
dotnet build --no-restore

echo "🚀 Starting Identity API..."
dotnet run --no-build &

echo "⏳ Waiting for API to start..."
sleep 5

echo "✅ Identity API restarted!"
echo ""
echo "Testing endpoints..."
echo ""
echo "📋 Available Auth endpoints:"
echo "  POST https://localhost:5002/api/auth/register"
echo "  POST https://localhost:5002/api/auth/login"
echo "  POST https://localhost:5002/api/auth/forgot-password"
echo "  POST https://localhost:5002/api/auth/reset-password"
echo ""
echo "🧪 To test forgot-password:"
echo "curl -k -X POST https://localhost:5002/api/auth/forgot-password \\"
echo "  -H 'Content-Type: application/json' \\"
echo "  -d '{\"email\":\"admin@shopnet.com\"}'"
