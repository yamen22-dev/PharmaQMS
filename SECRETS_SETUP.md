# 🔐 Secrets Configuration Guide (uitgebreid)

Dit project beschermt gevoelige informatie (database wachtwoorden, JWT keys, API keys) en dient deze NIET in Git op te slaan.

In deze gids leg ik uit hoe je lokaal werkt met:
- lokale `appsettings.*` bestanden (voor development)
- `dotnet user-secrets` (aanbevolen voor development)
- environment variables (voor productie of containers)

---

## Overzicht: wanneer wat gebruiken
- **Local config (`appsettings.Development.json`)**: snel en simpel voor één machine.
- **User Secrets**: veilig voor development, per-user en per-machine, niet in Git.
- **Environment Variables / Secret store**: gebruik in productie of containers.

---

## 1) User Secrets — stap-voor-stap (aanbevolen voor development)

Waarom: user-secrets slaan gevoelige waarden per gebruiker en per machine op buiten de repository.

Voordat je begint: open een terminal in de projectmap `PharmaQMS.API`.

1) Initialiseer user secrets (maakt een `UserSecretsId` aan in je project als die er nog niet is):
```bash
cd PharmaQMS.API
dotnet user-secrets init
```

2) Voeg een secret toe (voorbeeld voor connection string en JWT key):
```bash
dotnet user-secrets set "ConnectionStrings:AuthDb" "Server=localhost;Database=pharma_auth;User=dev;Password=YOUR_PASSWORD;"
dotnet user-secrets set "Jwt:Key" "your-super-secret-jwt-key-min-32-chars"
```

3) Controleer aanwezige secrets:
```bash
dotnet user-secrets list
```

4) Verwijder of clear secrets indien nodig:
```bash
dotnet user-secrets remove "Jwt:Key"
dotnet user-secrets clear
```

Waar worden ze opgeslagen (Windows):
```
%APPDATA%\\Microsoft\\UserSecrets\\<UserSecretsId>\\secrets.json
```
Op Linux/macOS is de map onder `~/.microsoft/usersecrets/`.

Hoe vind je de `UserSecretsId`?
- Open `PharmaQMS.API.csproj` en zoek naar het element `<UserSecretsId>`. Bijvoorbeeld:
```xml
   <PropertyGroup>
      <TargetFramework>net10.0</TargetFramework>
      <UserSecretsId>e8f1a2b3-...-abcd</UserSecretsId>
   </PropertyGroup>
```
Als het ontbreekt, voegt `dotnet user-secrets init` het automatisch toe.

Let op in code: de standaard WebApplication template voegt user-secrets automatisch toe in Development wanneer een `UserSecretsId` aanwezig is. Als je handmatig wilt toevoegen:
```csharp
builder.Configuration.AddUserSecrets<Program>(optional: true);
```

---

## 2) Voorbeelden & Nested keys
Je kunt genestelde keys gebruiken met `:` (dubbele punt). Dit mapt naar JSON-structuur.

Voorbeeld in `secrets.json` (intern opgeslagen):
```json
{
   "ConnectionStrings": {
      "AuthDb": "Server=localhost;Database=pharma_auth;User=dev;Password=...;"
   },
   "Jwt": {
      "Key": "super-secret-key-32+ chars"
   }
}
```

Of via CLI (zoals eerder):
```bash
dotnet user-secrets set "Smtp:Host" "smtp.example.com"
dotnet user-secrets set "Smtp:Port" "587"
```

---

## 3) Local files (`appsettings.json` / `.Development`) — snel maar voorzichtig
- Kopieer de voorbeeldbestanden en vul gevoelige waarden alleen lokaal in:
```bash
cd PharmaQMS.API
copy appsettings.Development.json.example appsettings.Development.json
# (Bewerk appsettings.Development.json in je editor en zet je lokale waarden)
```
- Zorg dat `appsettings*.json` niet gecommit wordt; de voorbeelden (`*.example`) wél.

---

## 4) Environment variables (prod / containers)

In production of Docker/CI gebruik je environment variables. Gebruik dubbele underscores `__` om nested keys te representeren:

Linux/macOS:
```bash
export ConnectionStrings__AuthDb="Server=prod;Database=...;User=...;Password=..."
export Jwt__Key="prod-secret-key-32-chars"
```

Windows PowerShell:
```powershell
$Env:ConnectionStrings__AuthDb = "Server=prod;Database=...;User=...;Password=..."
$Env:Jwt__Key = "prod-secret-key-32-chars"
```

Opmerking: user-secrets zijn NIET bedoeld voor productie of in containers — gebruik platform secrets (Azure Key Vault, AWS Secrets Manager) of env vars.

---

## 5) Configuratie-prioriteit
Wanneer de applicatie configuratie bouwt, is de typical precedence (hoog naar laag):
1. Environment variables
2. User Secrets (development)
3. appsettings.Development.json
4. appsettings.json

Dit betekent: een environment variable overschrijft user-secrets.

---

## 6) Visual Studio / Rider / IDE
- Visual Studio: rechts-klikken op het project > `Manage User Secrets` opent het `secrets.json` in een editor.
- In sommige IDEs kun je eenvoudig environment variables instellen in de run-config.

---

## 7) Docker / Containers
- User secrets werken niet automatisch in containers. Voor lokale container development gebruik environment variables of Docker secrets.
- Voor Docker Compose definieer je env-file of `secrets:` en mappend naar je container.

---

## 8) Troubleshooting
- Secrets niet zichtbaar in configuration: controleer of `UserSecretsId` in het `.csproj` staat en dat je in `Development` draait.
- `dotnet user-secrets list` geeft een fout: controleer dat je in de projectmap staat en dat `UserSecretsId` aanwezig is.
- Per ongeluk gecommitteerde secrets: verwijder ze uit Git history (bv. `git filter-repo` of `git filter-branch`) en roteer sleutels/passwords.

---

## 9) Veiligheidsregels (kort)
- DO: gebruik user-secrets voor development en env vars/secret stores voor productie.
- DO: commit alleen `*.example` bestanden.
- DON'T: commit echte wachtwoorden of keys.

---

## 10) Snel overzicht van handige commando's
```bash
cd PharmaQMS.API
dotnet user-secrets init        # initialiseer
dotnet user-secrets set "Key" "Value"   # zet een sleutel
dotnet user-secrets list        # toon alle keys voor dit project
dotnet user-secrets remove "Key"  # verwijder 1 key
dotnet user-secrets clear       # verwijder alle secrets voor dit project
```

---

Als je wilt, kan ik nu ook de hoofd-`README.md` bijwerken met een samenvatting en link naar dit bestand, of een korte stap-voor-stap in het Engels/Nederlands toevoegen voor ontwikkelaars in jouw team.

