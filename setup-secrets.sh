#!/bin/bash
# Setup script for PharmaQMS - Initializes secrets and local configuration
# Run from the project root: ./setup-secrets.sh

echo ""
echo "========================================"
echo "   PharmaQMS Secrets Setup"
echo "========================================"
echo ""

cd PharmaQMS.API

# Check if appsettings files already exist
if [ -f appsettings.json ]; then
    echo "[INFO] appsettings.json already exists - skipping"
else
    echo "[ACTION] Copying appsettings.json.example to appsettings.json"
    cp appsettings.json.example appsettings.json
    if [ $? -ne 0 ]; then
        echo "[ERROR] Failed to copy appsettings.json"
        exit 1
    fi
    echo "[OK] Created appsettings.json"
fi

if [ -f appsettings.Development.json ]; then
    echo "[INFO] appsettings.Development.json already exists - skipping"
else
    echo "[ACTION] Copying appsettings.Development.json.example to appsettings.Development.json"
    cp appsettings.Development.json.example appsettings.Development.json
    if [ $? -ne 0 ]; then
        echo "[ERROR] Failed to copy appsettings.Development.json"
        exit 1
    fi
    echo "[OK] Created appsettings.Development.json"
fi

# Initialize user-secrets if not already done
dotnet user-secrets list > /dev/null 2>&1
if [ $? -ne 0 ]; then
    echo "[ACTION] Initializing dotnet user-secrets..."
    dotnet user-secrets init
    if [ $? -ne 0 ]; then
        echo "[ERROR] Failed to initialize user-secrets"
        exit 1
    fi
    echo "[OK] User-secrets initialized"
else
    echo "[INFO] User-secrets already initialized"
fi

echo ""
echo "========================================"
echo "   NEXT STEPS"
echo "========================================"
echo ""
echo "1. Edit PharmaQMS.API/appsettings.json with your database credentials"
echo "2. Edit PharmaQMS.API/appsettings.Development.json (if developing locally)"
echo "3. Replace placeholder values like:"
echo "   - YOUR_SERVER"
echo "   - YOUR_USER"
echo "   - YOUR_PASSWORD"
echo "   - your-super-secret-jwt-key..."
echo ""
echo "4. IMPORTANT: DO NOT COMMIT appsettings.json files to Git!"
echo "   They are already in .gitignore"
echo ""
echo "5. Verify with: git status"
echo "   (appsettings.json should NOT appear)"
echo ""
echo "========================================"
echo "   Setup Complete!"
echo "========================================"
echo ""

cd ..
