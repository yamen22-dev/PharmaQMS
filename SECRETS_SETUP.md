# 🔐 Secrets Configuration Guide

## Setup Instructions (eerste keer)

Dit project beschermt gevoelige informatie (database passwords, JWT keys) door deze NIET in Git op te slaan.

### Optie 1: Met Local Configuration Files (Aanbevolen)

1. **Kopieer de example bestanden naar lokale versies:**
   ```bash
   cd PharmaQMS.API
   cp appsettings.json.example appsettings.json
   cp appsettings.Development.json.example appsettings.Development.json
   ```

2. **Bewerk `appsettings.json` met jouw echte waarden:**
   ```json
   "ConnectionStrings": {
     "AuthDb": "Server=localhost;Database=pharma_auth;User=root;Password=JOUW_WACHTWOORD;..."
   },
   "Jwt": {
     "Key": "JOUW_ECHTE_GEHEIME_JWT_KEY_MIN_32_CHARS"
   }
   ```

3. **Git zal deze bestanden automatisch negeren** (ze staan in `.gitignore`)

### Optie 2: Met User Secrets (Voor Development)

1. **Initialiseer user secrets voor het project:**
   ```bash
   cd PharmaQMS.API
   dotnet user-secrets init
   ```

2. **Stel secrets in:**
   ```bash
   dotnet user-secrets set "ConnectionStrings:AuthDb" "Server=localhost;Database=pharma_auth;User=root;Password=YOUR_PASSWORD;..."
   dotnet user-secrets set "Jwt:Key" "your-super-secret-jwt-key-min-32-chars"
   ```

3. **Secrets worden opgeslagen in `%APPDATA%\Microsoft\UserSecrets\` (Windows)**
   - Deze directory is al in `.gitignore`
   - Per machine en user uniek

### Optie 3: Environment Variables (Voor Production)

In production stel je environment variables in via je deployment platform:
```bash
export ConnectionStrings__AuthDb="Server=prod-server;Database=pharma_auth;User=...;Password=..."
export Jwt__Key="prod-secret-key-32-chars..."
```

## Veiligheidsregels

✅ **DO:**
- Kopieer `.example` bestanden naar lokale versies zonder `.example`
- Stel gevoelige waarden in op je lokale machine
- Gebruik `dotnet user-secrets` voor development
- Geef `.example` bestanden WEL in Git op

❌ **DON'T:**
- Commit bestanden met echte wachtwoorden
- Push gevoelige sleutels naar Git
- Deel database credentials in Slack of email
- Gebruik dezelfde JWT key in prod en dev

## Git Check

Verifieer dat je geen secrets per ongeluk hebt gecommit:
```bash
git status
```

Deze bestanden mogen NIET verschijnen:
- `appsettings.json`
- `appsettings.Development.json`
- `appsettings.*.local.json`

Enkel deze mogen in Git:
- `appsettings.json.example`
- `appsettings.Development.json.example`

## FAQ

**Q: Ik zag een wachtwoord in appsettings.json — is dit lek?**
A: Nee, zolang `appsettings.json` in `.gitignore` staat en niet gecommit is. Let op: als het al WEL gecommit is, moet je het verwijderen uit Git history!

**Q: Kan ik toch `appsettings.json` committen?**
A: Nee. Zet dit nooit in Git.

**Q: Hoe test ik mijn secrets lokaal?**
A: Zet ze in `appsettings.Development.json` of `dotnet user-secrets`, start de app en controleer logs.

