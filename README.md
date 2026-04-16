# PharmaQMS

PharmaQMS is a web-based pharmaceutical quality management system with:

- Backend: ASP.NET Core Web API (.NET 10)
- Frontend: Angular 21
- Auth: ASP.NET Core Identity + JWT + refresh tokens
- Dev database: SQLite (auto-created on startup)

## Local startup

## 1. Prerequisites

Install:

- .NET SDK 10
- Node.js 20+
- npm 10+

Optional but recommended:

- Trust the local ASP.NET development certificate

```bash
dotnet dev-certs https --trust
```

## 2. Setup Secrets (IMPORTANT!)

Before running the API, you must configure your local secrets. This ensures sensitive data (database credentials, JWT keys) are never committed to Git.

### Run the setup script:

**Windows:**
```bash
setup-secrets.bat
```

**Linux/Mac/Git Bash:**
```bash
bash setup-secrets.sh
```

This will:
- Create local `appsettings.json` from template
- Create local `appsettings.Development.json` from template
- Initialize `.NET user-secrets` for development

### Configure your secrets:

Edit `PharmaQMS.API/appsettings.json` and replace placeholders:
```json
"ConnectionStrings": {
  "AuthDb": "Server=YOUR_SERVER;Database=pharma_auth;User=YOUR_USER;Password=YOUR_PASSWORD;..."
},
"Jwt": {
  "Key": "your-32-character-minimum-secret-key-here"
}
```

**⚠️ IMPORTANT:** 
- Do NOT commit `appsettings.json` files to Git
- They are in `.gitignore` for security
- Never share your actual credentials in Git, Slack, or email

For detailed setup guide, see: [SECRETS_SETUP.md](SECRETS_SETUP.md)

## 3. Build & Run the API

## 3. Build & Run the API (HTTPS 7008)

From workspace root:

```bash
cd PharmaQMS.API
dotnet restore
dotnet watch run --launch-profile https
```

Expected API base URL:

- https://localhost:7008

Quick health check:

```bash
curl -k https://localhost:7008/api/v1/status
```

Notes:

- On startup, the API ensures database creation and seeds roles/users.
- In Development, MySQL is used (configured via secrets).
- All API requests are sanitized against XSS and injection attacks.

## 4. Start frontend SPA

Open a second terminal:

```bash
cd PharmaQMS.SPA
npm install
npm start
```

Expected SPA URL:

- http://localhost:4200

The SPA calls the API at:

- https://localhost:7008/api/v1

## 4. Start frontend SPA

Open a second terminal:

```bash
cd PharmaQMS.SPA
npm install
npm start
```

Expected SPA URL:

- http://localhost:4200

The SPA calls the API at:

- https://localhost:7008/api/v1

## Security Features

### Backend Sanitization (ASP.NET Core)
- All HTTP requests sanitized against XSS and injection attacks
- JSON payloads cleaned of dangerous characters and control characters
- Automatic via middleware (transparent to controllers)
- Attribute-level validation on DTOs
- See: [Backend sanitization details](SANITIZATION_BACKEND.md)

### Frontend Sanitization (Angular SPA)
- Automatic HTTP interceptor sanitizes all request/response payloads
- Angular `DomSanitizer` for safe HTML rendering
- Custom form validators with sanitization checks
- SafeHtmlPipe for template-level HTML safety
- See: [Frontend sanitization guide](SANITIZATION_SPA.md)

**Key features:**
- Removes HTML tags, JavaScript protocols, event handlers
- Prevents XSS attacks at both HTTP and DOM levels
- Works transparently with all API calls
- Custom validators available for form development

### Authentication & Authorization
- JWT Bearer tokens (15-minute expiration)
- Refresh token rotation with reuse detection
- 5 role-based access levels (QAManager, QCAnalyst, ProductionAnalyst, WarehouseOperator, Viewer)
- Four-eyes principle enforced for batch sign-offs (Sprint 2+)

### Secrets Management
- Database credentials stored locally, never in Git
- JWT signing keys managed via `user-secrets` or environment variables
- Example templates guide new developers
- Automatic setup scripts prevent misconfiguration

## Default test accounts

All seeded users use password:

- **Password@123**

⚠️ **IMPORTANT FOR DEVELOPMENT ONLY** - Change these passwords in production!

Accounts:

- qa.manager@pharmaqms.local (QAManager)
- qc.analyst@pharmaqms.local (QCAnalyst)
- production.analyst@pharmaqms.local (ProductionAnalyst)
- warehouse.operator@pharmaqms.local (WarehouseOperator)
- viewer@pharmaqms.local (Viewer)

## Auth endpoints

- POST /api/v1/auth/login
- POST /api/v1/auth/refresh
- POST /api/v1/auth/revoke
- GET /api/v1/status

Example login request:

```bash
curl -k -X POST https://localhost:7008/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"qa.manager@pharmaqms.local","password":"Password@123"}'
```

## Troubleshooting

### Secrets not configured

**Error:** `ConnectionStrings:AuthDb is missing`

**Fix:**
```bash
# Run setup script again
bash setup-secrets.sh
# Then edit PharmaQMS.API/appsettings.json with your database credentials
```

### Port already in use

If API fails to start with "address already in use", find and stop the process:

```bash
netnet -ano | findstr :7008
wmic process where processid=<PID> delete
```

Or for Linux/Mac:
```bash
lsof -i :7008
kill -9 <PID>
```

### HTTPS certificate errors

Run:

```bash
dotnet dev-certs https --trust
```

Then restart backend and frontend.

### CORS issues in browser

Backend is configured to allow SPA dev origins:

- http://localhost:4200
- https://localhost:4200

If you change SPA port, update CORS policy in `Program.cs`.

### Sanitization errors

If requests are rejected with "dangerous characters" error:

- The input likely contains HTML tags (`<`, `>`) or JavaScript protocols
- Remove special characters from your input
- Spaces, dots, hyphens, and underscores are safe

### Git accidentally tracked secrets

**NEVER push if this happens.** Immediately:

```bash
# Remove from Git history (dangerous! requires force push)
git rm --cached PharmaQMS.API/appsettings.json
git commit --amend

# Then rewrite history
git filter-branch --tree-filter 'rm -f PharmaQMS.API/appsettings.json'

# Only push if you're absolutely certain no one has pulled this commit
git push origin main --force-with-lease
```

**Better approach:** Ask your team to invalidate all credentials in that file immediately.

## Project Structure

```
PharmaQMS/
├── PharmaQMS.API/                   # ASP.NET Core backend
│   ├── Controllers/                 # HTTP endpoints
│   ├── Services/                    # Business logic
│   ├── Infrastructure/              # Sanitization, middleware
│   ├── Data/                        # Database contexts
│   ├── Models/                      # Entities & DTOs
│   ├── appsettings.json.example     # Template (commit)
│   ├── appsettings.Development.json.example
│   └── appsettings.json             # Local secrets (gitignore)
├── PharmaQMS.SPA/                   # Angular frontend
│   ├── src/
│   │   ├── app/
│   │   ├── environments/
│   │   └── main.ts
│   └── angular.json
├── SECRETS_SETUP.md                 # Secrets management guide
├── setup-secrets.sh / .bat          # Automation scripts
└── README.md                        # This file
```

## Documentation

- **Secrets Management:** [SECRETS_SETUP.md](SECRETS_SETUP.md)
- **Project Instructions:** [.github/copilot-instructions.md](.github/copilot-instructions.md)
- **API Documentation:** Available at `/api/openapi/v1` (Swagger)

## Tech Stack

| Component | Technology |
|-----------|-----------|
| Backend | ASP.NET Core 10, Entity Framework 9 |
| Database | MySQL (via Pomelo provider) |
| Authentication | ASP.NET Core Identity + JWT |
| Frontend | Angular 21, TypeScript |
| Logging | Serilog |
| API Docs | Scalar / Swashbuckle |
| Validation | Data Annotations + FluentValidation |
