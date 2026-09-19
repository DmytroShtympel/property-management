# PropertyManagement

A rental-application system for a property management company, built for a .NET technical assessment: ASP.NET Core MVC + Razor on .NET 10, EF Core (code-first) against SQL Server, ASP.NET Core Identity for auth. Applicants browse units and submit applications through a guided, multi-section wizard; property managers review, approve/return/deny, and approvals automatically issue a 12-month lease.

All 18 required functional requirements are implemented, along with all 5 optional bonus items: paginated/sorted grid + JSON API (OpenAPI at `/swagger`), review queue with claiming (`Under Review` status), property-manager-only notes, save-with-errors, and multi-applicant support with stale-save protection.

**Validation behaviour in the application wizard:** `Continue` is strict — an invalid section re-renders with per-field errors and is not saved. `Save & continue anyway` is the opt-in save-with-errors path: it persists an incomplete section, and the Summary then lists everything still blocking submission (with a "Fix this section" link back to each) while Submit stays disabled.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- A SQL Server instance. The default is **SQL Server LocalDB** (Windows; it ships with Visual Studio's ASP.NET / data workloads) and needs no configuration. Any SQL Server, SQL Server Express, or SQL Server in Docker works too — see [Using a different SQL Server](#using-a-different-sql-server).

No other tools are required — no Node, no separate frontend build step, no `dotnet ef` step.

## Running it

```bash
git clone https://github.com/DmytroShtympel/property-management.git
cd property-management
dotnet run --project PropertyManagement.Web
```

Then open **http://localhost:5117** in your browser (the default launch profile serves it there, in the `Development` environment) and sign in with one of the [demo accounts](#demo-accounts).

On first run the app:
1. Creates the `PropertyManagementDb` database and applies the EF Core migrations automatically (`Database.MigrateAsync()` on startup — no manual `dotnet ef database update` step).
2. Seeds it with Bogus: lookups, two property managers, two applicants (plus a few generated ones), properties and units, and applications in every status — including a `Loft` unit type marked **Inactive** that one seeded unit still uses, so the inactive-type rules are visible without any setup.

Seeding is idempotent, one guard per phase: roles and the four demo accounts are created only when missing, unit types only when that table is empty, and the generated properties/units/applications only when the database has no properties yet — so restarting never duplicates anything. To reset to a clean demo state, drop the database and start the app again.

### Demo accounts

All seeded accounts use the password **`Passw0rd1!`**. You can also sign up a new account — signup lets you pick either role.

| Email | Role | What it has in the seeded data |
|---|---|---|
| `applicant1@demo.local` | Applicant (Jordan Lee) | A Draft, a Returned (with the manager's comment), a Denied, an Under Review, and a co-applicant Submitted application |
| `applicant2@demo.local` | Applicant (Casey Kim) | A Submitted, an Approved (with its 12-month lease), a Withdrawn, an Under Review, and the co-applicant Submitted application |
| `manager1@demo.local` | Property Manager (Morgan Reyes) | Review Queue with unclaimed Submitted applications to claim, one application already claimed by this manager (Release), and one held by manager2 (view-only). Owns the unit that still uses the inactive `Loft` type (flagged "Inactive type" on its property's Units page) |
| `manager2@demo.local` | Property Manager (Priya Anand) | Same queue from the other side |

### Using a different SQL Server

The default connection string in `PropertyManagement.Web/appsettings.json` targets LocalDB (`Server=(localdb)\mssqllocaldb;Database=PropertyManagementDb;Trusted_Connection=True;...`). To use another instance, override it with an environment variable — no file edits needed:

```powershell
# PowerShell — SQL Server Express example
$env:ConnectionStrings__DefaultConnection = "Server=localhost\SQLEXPRESS;Database=PropertyManagementDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
dotnet run --project PropertyManagement.Web
```

```bash
# bash — SQL Server in Docker (also the option on macOS/Linux, where LocalDB does not exist)
docker run -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD='Your_password123' -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest
export ConnectionStrings__DefaultConnection='Server=localhost,1433;Database=PropertyManagementDb;User Id=sa;Password=Your_password123;TrustServerCertificate=True;MultipleActiveResultSets=true'
dotnet run --project PropertyManagement.Web
```

The database is created for you either way; the login only needs permission to create databases.

### Troubleshooting

- **`A network-related or instance-specific error` / LocalDB not found** — LocalDB is not installed (`sqllocaldb info` should list `MSSQLLocalDB`). Install it, or use the connection-string override above.
- **Port 5117 is already in use** — pick another: `dotnet run --project PropertyManagement.Web --urls http://localhost:5200`.
- **`/swagger` returns 404** — Swagger is only mapped in the `Development` environment, which the default launch profile sets. If you launch with `dotnet PropertyManagement.Web.dll` or `--no-launch-profile`, set `ASPNETCORE_ENVIRONMENT=Development`.
- The console line `Failed to determine the https port for redirect` is harmless when running over plain http.

### JSON API / OpenAPI docs (bonus)

The paginated/sorted applications grid is backed by `GET /api/applications` (sign in first — it uses the same cookie auth and returns `401`/`403` rather than a login redirect). It is documented in Swagger UI at **http://localhost:5117/swagger**.

## Running the tests

```bash
dotnet test
```

The suite has two tiers, and neither needs a database: `PropertyManagement.Tests/Domain` exercises business rules (status transitions, availability checks, validation, lease issuance) with no EF Core or ASP.NET Core involved; `PropertyManagement.Tests/Infrastructure` exercises persistence-shaped rules (database-side filtering, uniqueness, optimistic concurrency) against EF Core's InMemory provider. No test spins up the MVC pipeline.

## Project layout

```
PropertyManagement.Domain/          # Entities, enums, business-rule services — no framework deps
PropertyManagement.Infrastructure/  # EF Core DbContext, migrations, Identity wiring, Bogus seeding
PropertyManagement.Web/             # Controllers, view models, Razor views/partials/view components
PropertyManagement.Tests/           # xUnit — Domain and Infrastructure tiers
```
