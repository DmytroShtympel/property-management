# PropertyManagement

A rental-application system for a property management company, built for a .NET technical assessment: ASP.NET Core MVC + Razor on .NET 10, EF Core (code-first) against SQL Server, ASP.NET Core Identity for auth. Applicants browse units and submit applications through a guided, multi-section wizard; property managers review, approve/return/deny, and approvals automatically issue a 12-month lease.

All 18 required functional requirements are implemented, along with all 5 optional bonus items (paginated/sorted grid + JSON API, review queue with claiming, property-manager-only notes, save-with-errors, multi-applicant support).

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- SQL Server LocalDB (installed with Visual Studio's ASP.NET/data workloads) **or** SQL Server / SQL Server Express

No other tools are required — no Node, no separate frontend build step.

## Running it

```bash
git clone https://github.com/DmytroShtympel/property-management.git
cd property-management
dotnet run --project PropertyManagement.Web
```

That's it. On first run the app:
1. Creates the database and applies EF Core migrations automatically (`Database.MigrateAsync()` on startup — no manual `dotnet ef database update` step).
2. Seeds idempotently: lookups, two property managers, two applicants, properties/units, and applications in every status (including one seeded `Loft` unit type marked **Inactive**, so that behavior is visible without you needing to toggle it yourself).

Re-running the app (or restarting mid-seed) is safe — each seed phase checks for existing data before inserting, so nothing duplicates.

By default it connects to `(localdb)\mssqllocaldb`. To use a different SQL Server instance, update the `DefaultConnection` string in `PropertyManagement.Web/appsettings.json` (or `appsettings.Development.json`) before running.

### Demo accounts

All seeded accounts use the password **`Passw0rd1!`**.

| Email | Role |
|---|---|
| `manager1@demo.local` | Property Manager |
| `manager2@demo.local` | Property Manager |
| `applicant1@demo.local` | Applicant |
| `applicant2@demo.local` | Applicant |

You can also sign up a new account — signup lets you pick either role.

### Bonus: the JSON API / OpenAPI docs

The paginated/sorted applications grid (bonus item) is backed by `GET /api/applications`, documented via Swagger UI at **`/swagger`** when running in the `Development` environment.

## Running the tests

```bash
dotnet test
```

70 tests: `PropertyManagement.Tests/Domain` exercises business rules (status transitions, availability checks, validation, lease issuance) with no EF Core or ASP.NET Core involved; `PropertyManagement.Tests/Infrastructure` exercises persistence-shaped rules (database-side filtering, uniqueness, optimistic concurrency) against EF Core's InMemory provider. No test spins up the MVC pipeline.

## Project layout

```
PropertyManagement.Domain/          # Entities, enums, business-rule services — no framework deps
PropertyManagement.Infrastructure/  # EF Core DbContext, migrations, Identity wiring, Bogus seeding
PropertyManagement.Web/             # Controllers, view models, Razor views/partials/view components
PropertyManagement.Tests/           # xUnit — Domain and Infrastructure tiers
```
