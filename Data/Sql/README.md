# SQL Scripts

These SQL scripts are the hand-written equivalent of what EF Core's
migration produces. They exist to satisfy the assignment's requirement
to ship SQL artifacts and to document the schema independently of the
ORM.

## Order

1. `001_create_tables.sql` — `Providers` and `Licenses` tables, indexes,
   CHECK constraints, partial unique index for license-number-per-provider.
2. `002_create_views.sql` — three views encoding the soft-delete rule and
   the analytical scenarios called out in the assignment.
3. `003_seed_data.sql` — sample data covering every required scenario.

## Run from sqlite3 CLI

```bash
sqlite3 providers.db < 001_create_tables.sql
sqlite3 providers.db < 002_create_views.sql
sqlite3 providers.db < 003_seed_data.sql
```

## The application path

When the app starts, `DbInitializer.InitializeAsync` runs:

- `Database.MigrateAsync()` — applies any pending EF migration
- `EnsureViewsAsync()` — drops + re-creates the three views from raw SQL
- `SeedAsync()` — inserts the demo data (idempotent: only seeds an empty DB)

So either path (SQL scripts or the app) yields an equivalent database.
