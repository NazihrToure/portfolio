# Portfolio

Een bestaande portfoliosite met HTML, CSS en JavaScript en een ASP.NET Core Web API op .NET 10. Projecten en blogposts worden via Entity Framework Core opgeslagen in SQLite. SQLite wordt gebruikt met toestemming van de docent.

Opdracht 4 voegt ASP.NET Core Identity, cookie-authenticatie, de rollen Gast/Editor/Admin en xUnit-integratietests toe. De website en bestaande CRUD-routes blijven behouden.

## Benodigdheden

- .NET 10 SDK
- Node.js en npm
- Python 3
- curl voor de HTTP-voorbeelden

## Installatie

Voer vanuit de hoofdmap uit:

```sh
npm ci
dotnet restore Portfolio.slnx
dotnet tool restore
dotnet ef database update --project backend/Portfolio.Api
```

De database wordt niet meegeleverd. Voer de migraties uit voordat je de API start. Bij een bestaande database blijven projecten en blogposts behouden; de Identity-migratie voegt gebruikerstabellen toe. Maak bij bestaande belangrijke data eerst een lokale back-up.

## Starten

| Commando | Wat start er? |
|---|---|
| `npm run frontend` | Frontend op poort 2000 |
| `npm run backend` | API op poort 2001 |
| `npm run dev` | Beide tegelijk |

Frontend: <http://localhost:2000>. Swagger: <http://localhost:2001/swagger>. Stoppen kan met `Ctrl+C`.

De backend kan ook rechtstreeks starten:

```sh
dotnet run --project backend/Portfolio.Api
```

Het npm-backendscript gebruikt `$HOME/.dotnet/dotnet`. Staat .NET elders, gebruik dan het rechtstreekse `dotnet run`-commando of pas het script lokaal aan.

Het launchprofiel gebruikt Development. Alleen daarin zijn Swagger en het lokale rollencommando beschikbaar. Buiten Development vereisen authenticatiecookies HTTPS. Configureer HTTPS bij hosting; de lokale HTTP-URL is voor ontwikkeling.

## Codestructuur

| Map | Verantwoordelijkheid |
|---|---|
| `backend/Portfolio.Api/Controllers` | HTTP-verzoeken, toegangscontrole en statuscodes |
| `backend/Portfolio.Api/Services` | CRUD, DTO-mapping en lokale roltoekenning |
| `backend/Portfolio.Api/Data` | IdentityDbContext met portfolio- en gebruikerstabellen |
| `backend/Portfolio.Api/Models` | Project- en Blogpost-entities |
| `backend/Portfolio.Api/DTOs` | Gevalideerde invoer en responsegegevens |
| `backend/Portfolio.Api/Migrations` | Versiebeheer van de databasestructuur |
| `backend/Portfolio.Api.Tests` | xUnit-tests via de echte HTTP-pipeline |

De bestaande CRUD-stroom blijft `Controller → Service → DbContext → SQLite`. Identity beheert gebruikers, wachtwoordhashes, cookies en rollen.

## Database en migraties

De gebruikte connection string in `backend/Portfolio.Api/appsettings.json` is:

```json
{
  "ConnectionStrings": {
    "PortfolioDatabase": "Data Source=portfolio.db"
  }
}
```

Relatieve databasepaden worden ten opzichte van de API-contentroot opgelost. Bij `dotnet run --project backend/Portfolio.Api` staat de database in `backend/Portfolio.Api/portfolio.db`. Een eventueel lokaal aanwezige `DefaultConnection` wordt niet gebruikt door deze SQLite-API.

Een andere database kan via de omgevingsvariabele `ConnectionStrings__PortfolioDatabase` worden ingesteld. Zet geen wachtwoorden of secrets in configuratiebestanden in Git.

Na een datamodelwijziging:

```sh
dotnet ef migrations add NaamVanDeWijziging --project backend/Portfolio.Api
dotnet ef database update --project backend/Portfolio.Api
```

Controleer de migratie voordat je deze toepast. De API past migraties niet automatisch toe. De rollen Gast, Editor en Admin worden bij het opstarten aangemaakt als ze ontbreken. Er zijn geen standaardaccounts of standaardwachtwoorden.

## Registreren en inloggen

Onderstaande voorbeelden zijn voor bash/zsh, vanuit de hoofdmap, met een draaiende API. Ze bewaren cookies en ingevoerde gegevens tijdelijk buiten de repository. Het wachtwoord wordt verborgen ingevoerd en komt niet in de shellgeschiedenis terecht.

```sh
API_URL=http://localhost:2001
AUTH_DIR=$(mktemp -d)
export AUTH_DIR
trap 'rm -rf "$AUTH_DIR"' EXIT
python3 -c "
import getpass
import json
import os
from pathlib import Path
email = input('E-mailadres: ')
password = getpass.getpass('Wachtwoord: ')
path = Path(os.environ['AUTH_DIR']) / 'credentials.json'
path.write_text(json.dumps({'email': email, 'password': password}))
path.chmod(0o600)
"
refresh_csrf() {
  CSRF_TOKEN=$(curl --fail --silent --show-error -b "$AUTH_DIR/cookies" -c "$AUTH_DIR/cookies" "$API_URL/api/auth/csrf" | python3 -c 'import json,sys; print(json.load(sys.stdin)["token"])')
}
refresh_csrf
curl -i -b "$AUTH_DIR/cookies" -c "$AUTH_DIR/cookies" \
  -H 'Content-Type: application/json' -H "X-CSRF-TOKEN: $CSRF_TOKEN" \
  --data-binary "@$AUTH_DIR/credentials.json" "$API_URL/api/auth/register"
```

Registratie retourneert **201** met e-mailadres en de rol Gast. Je bent daarna nog niet ingelogd. Het wachtwoord moet minimaal tien tekens bevatten, inclusief een hoofdletter, kleine letter, cijfer en speciaal teken. E-mailadressen zijn uniek; ongeldige registratie retourneert **400**. Rollen uit registratie-invoer worden niet overgenomen.

Log vervolgens in met dezelfde gegevens:

```sh
curl -i -b "$AUTH_DIR/cookies" -c "$AUTH_DIR/cookies" \
  -H 'Content-Type: application/json' -H "X-CSRF-TOKEN: $CSRF_TOKEN" \
  --data-binary "@$AUTH_DIR/credentials.json" "$API_URL/api/auth/login"
refresh_csrf
curl -i -b "$AUTH_DIR/cookies" "$API_URL/api/auth/me"
```

Login retourneert **200** en een HttpOnly-cookie met SameSite=Strict. Onjuiste gegevens geven **401**. Na vijf mislukte wachtwoordpogingen wordt het account standaard vijf minuten geblokkeerd. `GET /api/auth/me` geeft e-mailadres en rollen terug.

POST, PUT en DELETE vereisen een `X-CSRF-TOKEN`-header met het token van `GET /api/auth/csrf` en de bijbehorende cookie. Haal na login opnieuw een token op: tokens zijn aan de huidige identiteit gekoppeld. Ontbrekende of ongeldige tokens geven **400** wanneer de gebruiker toegang tot de actie heeft. Autorisatie blijft eerder plaatsvinden: schrijfverzoeken zonder login geven **401**, als Gast **403**.

## Lokaal rollen toekennen

Registreer eerst een gebruiker. Voer vervolgens in een tweede terminal uit met diens e-mailadres:

```sh
dotnet run --project backend/Portfolio.Api -- assign-role gebruiker@example.com Editor
dotnet run --project backend/Portfolio.Api -- assign-role gebruiker@example.com Admin
```

Elk commando vervangt de huidige rol; de gebruiker heeft steeds precies één rol. Terugzetten kan met `Gast`. Het commando is herhaalbaar, vereist Development, weigert onbekende rollen of gebruikers en start geen webserver. Gebruik dezelfde databaseconfiguratie als de API.

Een rolwijziging trekt bestaande authenticatiecookies in. Log opnieuw in en haal een nieuw CSRF-token op. Gebruik hiervoor de logincommando's hierboven, voorafgegaan door `refresh_csrf`. Er is geen publiek endpoint waarmee gebruikers zichzelf een hogere rol kunnen geven.

## Gegevens wijzigen en uitloggen

Als Editor of Admin, na opnieuw inloggen en `refresh_csrf`:

```sh
curl -i -b "$AUTH_DIR/cookies" \
  -H 'Content-Type: application/json' -H "X-CSRF-TOKEN: $CSRF_TOKEN" \
  --data '{"title":"Mijn project","description":"Portfolio API","category":"Web","technologies":["C#","SQLite"]}' \
  "$API_URL/api/projects"
```

Gebruik voor PUT dezelfde velden en de URL `/api/projects/{id}`. Blogposts gebruiken `title`, `category`, `summary`, `content` en `publicationDate`. Swagger toont de DTO-schema's en publieke GET-routes. Voor beveiligde schrijfacties gebruiken deze voorbeelden curl met cookies en CSRF-header; Swagger injecteert die header niet automatisch.

Uitloggen:

```sh
curl -i -X POST -b "$AUTH_DIR/cookies" -c "$AUTH_DIR/cookies" \
  -H "X-CSRF-TOKEN: $CSRF_TOKEN" "$API_URL/api/auth/logout"
rm -rf "$AUTH_DIR"
```

## Routes en toegangsrechten

Onderstaande matrix geldt zowel voor `/api/projects` als `/api/blogposts`:

| Route | Niet ingelogd | Gast | Editor | Admin |
|---|---|---|---|---|
| `GET /api/{resource}` | 200 | 200 | 200 | 200 |
| `GET /api/{resource}/{id}` | 200/404 | 200/404 | 200/404 | 200/404 |
| `POST /api/{resource}` | 401 | 403 | 201/400 | 201/400 |
| `PUT /api/{resource}/{id}` | 401 | 403 | 204/400/404 | 204/400/404 |
| `DELETE /api/{resource}/{id}` | 401 | 403 | 204/400/404 | 204/400/404 |

Schrijfacties vereisen geldige invoer en een geldig CSRF-token. Een niet-bestaand ID geeft bij geldige, toegestane verzoeken 404. Een POST-response bevat het gemaakte item en de Location-header; PUT en DELETE geven 204 zonder responsebody.

| Authenticatieroute | Toegang | Resultaat |
|---|---|---|
| `GET /api/auth/csrf` | Publiek | 200, token en antiforgery-cookie |
| `POST /api/auth/register` | Publiek, CSRF vereist | 201 of 400 |
| `POST /api/auth/login` | Publiek, CSRF vereist | 200, 400 of 401 |
| `GET /api/auth/me` | Ingelogd | 200 of 401 |
| `POST /api/auth/logout` | Ingelogd, CSRF vereist | 204, 400 of 401 |

De bestaande frontend gebruikt alleen publieke GET-verzoeken. `Cors:AllowedOrigins` bepaalt welke frontendadressen deze mogen uitlezen. Cross-origin cookie-login vanuit de statische frontend is niet toegevoegd; gebruik voor beheer de API rechtstreeks. Er is geen nieuw dashboard.

## Tests uitvoeren

```sh
dotnet build Portfolio.slnx
dotnet test Portfolio.slnx
```

De 30 xUnit-testgevallen gebruiken per test een eigen in-memory SQLite-database met de echte migraties. Ze wijzigen geen lokale portfoliodatabase. De HTTP-tests gebruiken WebApplicationFactory, echte Identity-gebruikers en cookies; authenticatie wordt niet nagebootst.

Getest worden:

- CRUD voor beide resources als Editor en Admin, response-inhoud en databasewijzigingen.
- Publieke GET-routes, 401 zonder login en 403 als Gast, zonder ongewenste datamutaties.
- Ongeldige invoer, project-URL's, null-technologielijsten en ontbrekende IDs.
- Registratie, unieke e-mailadressen, wachtwoordhashing en automatische Gast-toekenning.
- Login, cookie-eigenschappen, logout, mislukte login en accountblokkering.
- CSRF-bescherming, herhaalbare roltoekenning en intrekking van sessies bij rolwijzigingen.
- Behoud van bestaande portfolio-inhoud bij de Identity-migratie.

## Frontendbuild

```sh
npm run build
```

Dit kopieert de bestaande frontend naar `build`. Voor online gebruik zijn daarnaast een gehoste backend en de juiste API-URL of doorverwijzing nodig. HTML, CSS en JavaScript zijn voor opdracht 4 ongewijzigd gebleven.
