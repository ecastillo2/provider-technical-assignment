# Provider Management — Take-Home Assignment

ASP.NET Core MVC (.NET 8) application for managing **Providers** and their
**Licenses**, with **soft-delete enforced at the database, query, and
interceptor layers** and a small read-only Razor + Chart.js dashboard.

This document explains how to run the project, the design decisions, and
the trade-offs made.

---

## 1. How to run locally

### Prerequisites
- .NET 8 SDK
- (Optional) `sqlite3` CLI if you want to inspect the database file by hand

### Run
```bash
git clone <repo-url>
cd ProviderAssignmentStarter
git checkout <feature-branch>
dotnet restore
dotnet run
```

On first run, the application will:

1. Create `Data/providers.db` (if missing) from the EF Core model.
2. Create the analytical views (`vw_ActiveProviders`, etc.) via raw SQL.
3. Seed ~8 providers and ~9 licenses, plus one soft-deleted provider so
   the **Audit** page has something to show out of the box.

Open https://localhost:7xxx (port shown in the console) and navigate via
the top nav — **Dashboard**, **Providers**, **Audit (Deleted)**.

If you prefer EF migrations over `EnsureCreated()`, see
[Database delivery options](#5-database-delivery) below.

---

## 2. Solution structure

A single project, organised by concern with clear inward-pointing
dependencies. Each folder maps to a layer; controllers depend on
services, services on repositories, repositories on EF Core.

```
/
├── Domain/                       # POCO entities, enums, base interfaces
│   ├── Common/
│   │   ├── ISoftDeletable.cs
│   │   ├── IAuditable.cs
│   │   └── BaseEntity.cs
│   ├── Enums/                    # ProviderStatus, LicenseStatus
│   └── Entities/                 # Provider, License
├── Infrastructure/
│   ├── Data/
│   │   ├── AppDbContext.cs
│   │   ├── DbInitializer.cs      # EnsureCreated + view DDL + seed
│   │   └── Configurations/       # IEntityTypeConfiguration<T> per entity
│   ├── Interceptors/
│   │   ├── SoftDeleteInterceptor.cs
│   │   └── AuditableInterceptor.cs
│   └── Repositories/             # IProviderRepository, ILicenseRepository
├── Services/                     # IProviderService, ILicenseService, IDashboardService
│   └── Mapping/                  # Hand-rolled entity → VM mapping
├── ViewModels/                   # Form / list / detail / dashboard VMs
├── Controllers/                  # Thin MVC controllers
├── Views/                        # Razor views (Providers, Licenses, Dashboard, Home)
├── Data/
│   ├── providers.db              # Pre-built DB (committed for reviewer convenience)
│   └── Sql/                      # Hand-written DDL + seed scripts (mirror EF model)
└── Program.cs
```

**Why a single project, not a multi-project solution?** For a take-home
of this size, multi-project sprawl adds setup friction without providing
an SoC benefit beyond namespaces and folder boundaries. The folder
structure already enforces strict dependency directions: nothing in
`Domain` references `Infrastructure`; `Controllers` never reference EF
Core directly. If the codebase grew, splitting into `Domain.csproj`,
`Infrastructure.csproj`, and `Web.csproj` would be a mechanical refactor.

---

## 3. Database design decisions

### Schema

| Table | Purpose | Notable design choices |
|---|---|---|
| `Providers` | Aggregate root | Integer PK (SQLite-native, fastest joins); status as TEXT with CHECK constraint; soft-delete columns inline (no separate audit table for take-home brevity) |
| `Licenses` | Child of Provider | FK with `ON DELETE RESTRICT` (we never hard-delete); status as TEXT with CHECK; expiration date indexed for "expiring soon" queries; partial unique index on `(ProviderId, LicenseNumber)` *where IsDeleted = 0* so a number can be reused after a soft-delete |

### Enums as text

Both `ProviderStatus` and `LicenseStatus` are persisted as text rather
than integers. Reasoning:

- The database stays human-readable when poked with `sqlite3` CLI.
- A `CHECK` constraint can guard the column directly.
- Adding a new status doesn't risk renumbering an existing one.

### Indexes

Indexes are designed around the hot paths the application actually uses:

- `IX_Providers_IsDeleted_ProviderName` — listing grid, ordered by name.
- `IX_Providers_IsDeleted_Status` — status filter.
- `IX_Licenses_ProviderId_IsDeleted` — license list per provider.
- `IX_Licenses_ExpirationDate` — dashboard's "expiring soon" query.
- `UX_Licenses_ProviderId_LicenseNumber_Active` — partial unique index
  enforcing per-provider license-number uniqueness only among non-deleted
  rows.

### Views

Three SQL views mirror the assignment's required scenarios. They are
created from raw SQL by `DbInitializer.EnsureViewsAsync` because EF Core
has no first-class view support:

- `vw_ActiveProviders`
- `vw_ActiveProvidersWithActiveLicenses`
- `vw_ActiveProvidersWithExpiredLicenses`

**Where the application actually queries these scenarios** — the views
exist as documentation and as a manual-SQL escape hatch. The C# code
expresses the same predicates in LINQ inside `ProviderRepository` and
`DashboardService`, so the queries remain testable and refactor-safe.
Both produce identical results for the seeded data.

---

## 4. Soft-delete implementation

The most heavily evaluated area of the assignment. The implementation
uses **defence-in-depth**: three independent layers, each of which would
catch a hard-delete bug on its own.

### Layer 1 — Domain contract (`ISoftDeletable`)

Every persisted entity inherits `BaseEntity`, which implements
`ISoftDeletable` and `IAuditable`. The columns (`IsDeleted`,
`DeletedDate`, `DeletedBy`, `CreatedDate`, `ModifiedDate`) are guaranteed
to exist on every aggregate.

### Layer 2 — Global query filter (`AppDbContext`)

```csharp
modelBuilder.Entity<Provider>().HasQueryFilter(p => !p.IsDeleted);
modelBuilder.Entity<License>().HasQueryFilter(l => !l.IsDeleted);
```

Every LINQ query against the DbContext silently appends
`WHERE IsDeleted = 0`. Standard reads cannot accidentally surface a
deleted record. To opt out (audit / restore / debugging) callers must
explicitly call `.IgnoreQueryFilters()` — the intent is visible at the
call site.

### Layer 3 — `SaveChangesInterceptor` (`SoftDeleteInterceptor`)

The interceptor watches every `SaveChanges` for entities in
`EntityState.Deleted` and:

1. Flips the state to `Modified`.
2. Sets `IsDeleted = true`, `DeletedDate = UtcNow`, `DeletedBy = "system"`.
3. **Cascades to children** — when the deleted entity is a `Provider`,
   the interceptor soft-deletes its loaded Licenses **and** issues an
   `ExecuteUpdate` to flag any unloaded Licenses in a single round-trip.
   Callers do not have to remember to eager-load Licenses before deleting.

This means **`_db.Providers.Remove(provider)` just works** — the rest of
the codebase looks like normal EF code; the no-hard-delete policy is
enforced beneath it.

### Cascade-soft-delete on Providers

Confirmed scope decision (per assignment review):

> When a Provider is soft-deleted, its Licenses are also soft-deleted in
> the same `SaveChanges` call.

This mirrors how state agencies actually retire providers — licenses
should not survive the parent. `ProviderRepository.RestoreAsync` reverses
the cascade so a restored provider gets its licenses back too.

### Audit / admin escape hatch

```csharp
var deleted = await _db.Providers
    .IgnoreQueryFilters()
    .Where(p => p.IsDeleted)
    .ToListAsync();
```

`ProviderRepository.GetDeletedAsync()` and
`GetByIdIncludingDeletedAsync()` are the only places that call
`IgnoreQueryFilters()`. They power the **Audit (Deleted)** page and the
**Restore** action.

### Where the filter lives — DB vs application

| Layer | What enforces it | Why |
|---|---|---|
| Database | CHECK constraints on enum columns; FK ON DELETE RESTRICT | Defence-in-depth: even raw SQL must follow the contract. |
| EF model | Global query filter on every soft-deletable entity | Prevents accidental leaks from any LINQ query. |
| Application | `SaveChangesInterceptor` rewrites Deleted → Modified | Makes hard-delete impossible from C# code. |

---

## 5. Database delivery

The repository ships **two ways to get a working database**:

1. **Pre-built `Data/providers.db`** — committed to the repo for
   immediate reviewer use. Cloning + `dotnet run` works without any
   extra setup.

2. **Auto-create on first run** — `DbInitializer.InitializeAsync` calls
   `Database.EnsureCreatedAsync()`. If the `.db` file is deleted the
   application will rebuild it from the EF model on next start, then
   apply the views and seed data.

If you prefer a migration-history-based workflow, run:

```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate
```

…then change `EnsureCreatedAsync()` to `MigrateAsync()` in
`DbInitializer.cs`. Everything else continues to work.

The hand-written SQL in `Data/Sql/` provides the same schema as a third
path for reviewers who want to inspect the DDL without diving into EF.

---

## 6. Required data scenarios — where they live

| Scenario | Implementation |
|---|---|
| Active providers, soft-deleted excluded | `IProviderService.ListAsync()` → `ProviderRepository.GetAllAsync()`. The global query filter handles soft-delete exclusion automatically. |
| Active providers + their active licenses | `ProviderRepository.GetActiveWithActiveLicensesAsync()` — also expressed in `vw_ActiveProvidersWithActiveLicenses`. |
| Active providers with expired licenses ("looks active but isn't") | `ProviderRepository.GetActiveWithExpiredLicensesAsync()` and `vw_ActiveProvidersWithExpiredLicenses`. Surfaced on the Dashboard as a top-level metric. |
| Audit / soft-deleted records | `ProviderRepository.GetDeletedAsync()` (uses `.IgnoreQueryFilters()`). Surfaced via `Providers/Deleted` and `Providers/AuditDetails/{id}`. |

---

## 7. Application architecture

### Dependency direction (inward only)

```
Controllers  ──>  Services  ──>  Repositories  ──>  AppDbContext (EF)
   │                │                │
   └────────────────┴────────────────┴─── ViewModels (read) / Domain Entities (persistence)
```

- **Controllers** are thin. They validate `ModelState`, delegate to a
  service, and return the view. No EF, no business rules.
- **Services** orchestrate. They take VMs in and return VMs out — Domain
  entities never leak to the web layer. Business rules live here.
- **Repositories** abstract EF Core. Services depend on
  `IProviderRepository`/`ILicenseRepository`, not on `DbContext`. This
  is the seam at which a unit test would substitute an in-memory fake.
- **Domain** is pure POCO. No EF references; no infrastructure concerns.
- **Interceptors** are the cross-cutting layer that enforces the
  soft-delete and audit-stamp invariants for *every* call to
  `SaveChanges`, regardless of which service initiated the change.

### SOLID points worth calling out

- **S** — Each class has one reason to change. The `SoftDeleteInterceptor`
  knows about deletion only; the `AuditableInterceptor` knows about
  audit timestamps only. They are composed at registration time.
- **O** — Adding a new soft-deletable entity requires only implementing
  `ISoftDeletable` (or extending `BaseEntity`) and adding a query filter.
  No interceptor changes.
- **L** — Every entity behaves the same with respect to soft-delete and
  audit because the contract is in the base type.
- **I** — `IProviderRepository` exposes the methods the service actually
  calls; it is not a generic `IRepository<T>` super-interface.
- **D** — Services depend on repository **abstractions**. Controllers
  depend on service **abstractions**. Concrete bindings live only in
  `Program.cs`.

---

## 8. Dashboard (Razor + Chart.js)

A read-only Razor page at `/Dashboard` showing:

- **Summary cards**: total providers, active providers, total licenses,
  count of "active providers w/ expired licenses".
- **Doughnut chart**: providers by lifecycle status.
- **Bar chart**: licenses by validity (active / expired / suspended).
- **Expiring-soon table**: licenses expiring in the next 30 days.
- **Active-but-expired list**: links into Provider Details.

All metrics are computed by `DashboardService` in C#. The view receives
a fully-populated `DashboardVm` and only renders. Per the assignment:
*business logic must not exist only in the UI*. Chart.js is loaded from
the jsDelivr CDN — no extra build step.

---

## 9. Assumptions & trade-offs

- **No authentication.** `DeletedBy` is hardcoded to `"system"`. In a
  production system this would come from `HttpContext.User`.
- **Cascade soft-delete on Providers** is intentional (per assignment
  review). Licenses do not survive their parent. `RestoreAsync` reverses
  the cascade.
- **No paging on the listing.** Acceptable for the seeded volume; trivial
  to add via `Skip`/`Take` and a paged VM.
- **EnsureCreated over migrations.** Chosen for reviewer friction. A real
  project would use migrations; the README documents the one-line switch.
- **Hand-rolled mapping (not AutoMapper).** Two aggregates, six VMs —
  AutoMapper would obscure rather than reduce code.
- **Views recreated on every startup.** Cheap on SQLite; keeps the view
  definition in lock-step with code without a migration step.
- **Dashboard runs ~6 small COUNTs** rather than one big query. Negligible
  on the seeded set; would be reworked into a single CTE-backed query
  if data volume grew.

---

## 10. What I would improve with more time

- **Real authentication / authorisation** so `DeletedBy` is meaningful
  and the Audit page is gated to admins only.
- **Dedicated `AuditLog` table** capturing every state change with old
  and new values, instead of just the `IsDeleted`/`DeletedDate` columns.
- **Server-side paging, sorting, and column filters** on the listing.
- **Integration tests** against the SQLite file — round-trip through the
  interceptor to assert hard-deletes are impossible and the cascade fires.
- **Unit tests** for `ProviderService` using a fake repository, and for
  the soft-delete query filter behaviour.
- **Generated EF migration** instead of `EnsureCreated`, with a CI step
  that verifies the migration produces the same schema as the model.
- **Health-check + structured logging** (Serilog) for production-support.
- **Docker Compose** so a reviewer with no .NET SDK can still
  `docker compose up`.

---

## 11. AI usage disclosure

In line with the assignment's section 2: AI assistance was used during
this build (code drafting, schema review, README scaffolding). Every
file in this repo has been read, refined, and is something I am prepared
to defend in the follow-up technical discussion. The architectural
choices, schema design, soft-delete strategy, and trade-offs above are
mine and are explained on their own merits rather than as
AI-generated artefacts.
