# Take-Home Submission · Provider / License Management

ASP.NET Core MVC (.NET 8) + EF Core + SQLite, with **soft-delete enforced
at three layers**, a Razor + Chart.js dashboard, and a 20-test xUnit
suite that runs against real SQLite (in-memory).

> Full architectural detail is in [`README.md`](../README.md). This PR
> description is a short tour for the reviewer.

---

## What was built

- **Domain layer** (`/Domain`) — `Provider`, `License`, `BaseEntity`,
  `ISoftDeletable`, `IAuditable`, status enums. Pure POCOs, no EF.
- **Infrastructure layer** (`/Infrastructure`) — `AppDbContext` with
  global query filters, `IEntityTypeConfiguration` per aggregate,
  `SoftDeleteInterceptor` (with cascade to children),
  `AuditableInterceptor`, repositories.
- **Service layer** (`/Services`) — orchestration; controllers depend on
  service abstractions, not on EF.
- **Razor MVC web layer** (`/Controllers`, `/Views`) — full Provider CRUD,
  audit listing + restore, License CRUD nested under Provider, dashboard.
- **Validation** — custom `[NotInPast]` attribute on `LicenseEditVm`
  with edit-mode skip behaviour.
- **Search/filter** on the Providers listing (querystring-bound,
  SQL-side via `EF.Functions.Like`).
- **SQL artifacts** (`/Data/Sql/`) — hand-written DDL for tables, views,
  and seed; mirrors the EF model.
- **Test suite** (`/Tests/ProviderAssignmentStarter.Tests/`) — 20 tests
  covering soft-delete, cascade, audit, scenario queries, and the
  validation attribute.

---

## Soft-delete (the headline area)

Three independent layers — any one of them would catch a hard-delete bug
on its own.

| Layer | Mechanism | Role |
|---|---|---|
| Domain | `ISoftDeletable` on `BaseEntity` | Every aggregate carries `IsDeleted`, `DeletedDate`, `DeletedBy` |
| EF model | Global query filter | Standard reads silently exclude deleted rows |
| Persistence | `SaveChangesInterceptor` | Rewrites `Deleted` → `Modified` and **cascades to Licenses** |

`_db.Providers.Remove(provider)` *just works* — the rest of the codebase
looks like normal EF; the no-hard-delete policy is invisible above it.
Audit / restore go through `.IgnoreQueryFilters()` at the repository.

---

## How to verify (5 minutes)

```bash
git checkout <this-branch>
dotnet restore
dotnet test                 # 20 tests, ~2s
dotnet run --project ProviderAssignmentStarter.csproj
```

Open the URL the console prints, then walk through:

1. **Providers** — listing shows 7 (one is pre-soft-deleted in seed). Try the
   search box ("DeKalb") and the Status filter.
2. **Audit (Deleted)** — shows the pre-deleted provider; click **Restore**;
   confirm it returns to the main listing with its licenses.
3. **Dashboard** — Chart.js charts render; "Active providers w/ expired
   licenses" card shows the seeded "DeKalb Senior Services" (active
   provider whose only license expired by date — the spec scenario).
4. **Create / Edit / Delete** a provider. Edit a license; confirm the
   `[NotInPast]` validation rejects past dates on Create but allows them
   on Edit (so historical data can still be corrected).

---

## Where to look in the code

| Concern | File |
|---|---|
| Soft-delete enforcement | `Infrastructure/Interceptors/SoftDeleteInterceptor.cs` |
| Global query filters | `Infrastructure/Data/AppDbContext.cs` |
| Audit escape hatch | `Infrastructure/Repositories/ProviderRepository.cs` (`GetDeletedAsync`, `GetByIdIncludingDeletedAsync`, `RestoreAsync`) |
| Required scenario queries | `ProviderRepository.GetActiveWithActiveLicensesAsync` / `GetActiveWithExpiredLicensesAsync` |
| Hand-written SQL | `Data/Sql/{001_create_tables, 002_create_views, 003_seed_data}.sql` |
| Tests for soft-delete | `Tests/.../Infrastructure/SoftDeleteInterceptorTests.cs` |
| Custom validation | `ViewModels/Validation/NotInPastAttribute.cs` |

---

## Key trade-offs (full list in README §10)

- **`EnsureCreated` over migrations** for reviewer friction — README
  documents the one-line switch to `MigrateAsync()`.
- **Cascade soft-delete on Provider** is intentional. `RestoreAsync`
  reverses the cascade.
- **Single project, folder-based SoC** rather than multi-csproj —
  pragmatic for the assignment scope.
- **`DeletedBy = "system"`** since there is no auth in scope.

---

## AI usage

Per the assignment's section 2: AI assistance was used during drafting
(code, schema review, README scaffolding). Every file has been read,
refined, and is something I am prepared to defend in the follow-up
discussion.
