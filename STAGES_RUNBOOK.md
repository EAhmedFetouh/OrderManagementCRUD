# OrderManagement.CRUD Stages Runbook

This runbook applies one stage at a time using a separate database name per stage.

## 0) One-time setup

```bash
git init
git checkout -b step/01-crud-baseline
```

Create the first commit so branches can be created and persisted:

```bash
git add .
git commit -m "baseline CRUD demo setup"
```

## 1) Stage 1 - Baseline

Branch:

```bash
git checkout step/01-crud-baseline
```

Set connection string database name to:

- `OrderCrud_S1_Baseline`

Run:

```bash
dotnet ef database update
dotnet run
```

Test:

- `GET /api/dashboard`
- `POST /api/benchmarks/run`

## 2) Stage 2 - Indexes

Create and switch:

```bash
git checkout -b step/02-add-indexes
```

Set connection string database name to:

- `OrderCrud_S2_Indexes`

Run:

```bash
dotnet ef migrations add AddPerformanceIndexes
dotnet ef database update
dotnet run
```

Test:

- `GET /api/dashboard`
- `POST /api/benchmarks/run`

## 3) Stage 3 - Caching

Create and switch:

```bash
git checkout -b step/03-add-caching
```

Set connection string database name to:

- `OrderCrud_S3_Cache`

Set in `DemoSettings`:

- `EnableDashboardCaching = true`

Run:

```bash
dotnet ef database update
dotnet run
```

Test:

- `GET /api/dashboard` (MISS then HIT)
- `POST /api/orders/with-cache-check`
- `POST /api/benchmarks/run`

## 4) Stage 4 - Materialized View

Create and switch:

```bash
git checkout -b step/04-add-materialized-view
```

Set connection string database name to:

- `OrderCrud_S4_MatView`

Run:

```bash
dotnet ef migrations add AddDashboardMaterializedView
dotnet ef database update
dotnet run
```

Test:

- `GET /api/dashboard`
- `GET /api/dashboard/compare`
- `POST /api/benchmarks/materialized-impact`
- `POST /api/benchmarks/run`

