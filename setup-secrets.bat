@echo off
REM Setup script voor PharmaQMS - Initializes secrets and local configuration
REM Run from the project root: setup-secrets.bat

echo.
echo ========================================
echo   PharmaQMS Secrets Setup
echo ========================================
echo.

cd PharmaQMS.API

REM Check if appsettings files already exist
if exist appsettings.json (
    echo [INFO] appsettings.json already exists - skipping
) else (
    echo [ACTION] Copying appsettings.json.example to appsettings.json
    copy appsettings.json.example appsettings.json
    if errorlevel 1 (
        echo [ERROR] Failed to copy appsettings.json
        exit /b 1
    )
    echo [OK] Created appsettings.json
)

if exist appsettings.Development.json (
    echo [INFO] appsettings.Development.json already exists - skipping
) else (
    echo [ACTION] Copying appsettings.Development.json.example to appsettings.Development.json
    copy appsettings.Development.json.example appsettings.Development.json
    if errorlevel 1 (
        echo [ERROR] Failed to copy appsettings.Development.json
        exit /b 1
    )
    echo [OK] Created appsettings.Development.json
)

REM Initialize user-secrets if not already done
dotnet user-secrets list >nul 2>&1
if errorlevel 1 (
    echo [ACTION] Initializing dotnet user-secrets...
    dotnet user-secrets init
    if errorlevel 1 (
        echo [ERROR] Failed to initialize user-secrets
        exit /b 1
    )
    echo [OK] User-secrets initialized
) else (
    echo [INFO] User-secrets already initialized
)

echo.
echo ========================================
echo   NEXT STEPS
echo ========================================
echo.
echo 1. Edit PharmaQMS.API\appsettings.json with your database credentials
echo 2. Edit PharmaQMS.API\appsettings.Development.json (if developing locally)
echo 3. Replace placeholder values like:
echo    - YOUR_SERVER
echo    - YOUR_USER
echo    - YOUR_PASSWORD
echo    - your-super-secret-jwt-key...
echo.
echo 4. IMPORTANT: DO NOT COMMIT appsettings.json files to Git!
echo    They are already in .gitignore
echo.
echo 5. Verify with: git status
echo    (appsettings.json should NOT appear)
echo.
echo ========================================
echo   Setup Complete!
echo ========================================
echo.

cd ..
