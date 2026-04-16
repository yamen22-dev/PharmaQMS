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

## 2. Start backend API (HTTPS 7008)

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
- In Development, SQLite is used with file `pharma_auth.db`.

## 3. Start frontend SPA

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

## Default test accounts

All seeded users use password:

- Password@123

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

## Port already in use

If API fails to start with "address already in use", find and stop the process:

```bash
netstat -ano | findstr :7008
netstat -ano | findstr :5096
wmic process where processid=<PID> delete
```

## HTTPS certificate errors

Run:

```bash
dotnet dev-certs https --trust
```

Then restart backend and frontend.

## CORS issues in browser

Backend is configured to allow SPA dev origins:

- http://localhost:4200
- https://localhost:4200

If you change SPA port, update CORS policy in `Program.cs`.
