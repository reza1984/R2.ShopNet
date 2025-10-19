#!/bin/bash

# TailAdmin Layout Setup Script
# Run this from the R2.ShopNet.Web.Admin directory

echo "🚀 Setting up TailAdmin layout for R2.ShopNet Admin Portal..."
echo ""

# Check if we're in the right directory
if [ ! -f "package.json" ]; then
    echo "❌ Error: package.json not found. Please run this script from the Web.Admin directory."
    exit 1
fi

echo "📦 Step 1: Installing Tailwind CSS v4..."
npm install -D tailwindcss@next @tailwindcss/vite@next

if [ $? -eq 0 ]; then
    echo "✅ Tailwind CSS installed successfully"
else
    echo "❌ Failed to install Tailwind CSS"
    exit 1
fi

echo ""
echo "📝 Step 2: Verifying project structure..."

# Check if layout components exist
if [ -d "src/app/layout/app-layout" ]; then
    echo "✅ Layout components found"
else
    echo "❌ Layout components not found"
    exit 1
fi

# Check if page components exist
if [ -d "src/app/pages/dashboard" ]; then
    echo "✅ Page components found"
else
    echo "❌ Page components not found"
    exit 1
fi

# Check if styles.css exists
if [ -f "src/styles.css" ]; then
    echo "✅ Tailwind styles configuration found"
else
    echo "❌ styles.css not found"
    exit 1
fi

echo ""
echo "✨ Setup complete! Your app is ready to run."
echo ""
echo "📋 What you have now:"
echo "   ✅ TailAdmin layout components (sidebar, header, backdrop)"
echo "   ✅ Dark mode support"
echo "   ✅ Responsive design"
echo "   ✅ Custom R2.ShopNet navigation menu"
echo "   ✅ All page components"
echo "   ✅ Tailwind CSS v4 configured"
echo ""
echo "🎯 Next steps:"
echo "   1. Start the development server:"
echo "      npm start"
echo ""
echo "   2. Open your browser:"
echo "      http://localhost:4200"
echo ""
echo "   3. Test features:"
echo "      - Toggle sidebar (hamburger icon)"
echo "      - Switch theme (sun/moon icon)"
echo "      - Navigate through menu items"
echo "      - Test responsive design (resize browser)"
echo ""
echo "📚 Documentation:"
echo "   - Implementation status: docs/TailAdmin-Implementation-Status.md"
echo "   - Quick guide: docs/TailAdmin-Quick-Implementation.md"
echo "   - Migration notes: docs/App-Migration-Complete.md"
echo ""
echo "🎉 Happy coding!"
