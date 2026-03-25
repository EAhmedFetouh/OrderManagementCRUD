# OrderManagement.CRUD Stages Runbook

This runbook applies one stage at a time using a **separate database name per stage** so comparisons stay fair.

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

## 2a) Stage 2a - Misaligned indexes (FK-only)

**Story:** “We added indexes” — classic **foreign-key indexes only** (`CustomerId`, `OrderId`, `ProductId`). They help FK lookups but barely help the heavy dashboard aggregations, while **increasing write cost**.

Branch:

```bash
git checkout step/02-indexes-misaligned
```

Set connection string database name to:

- `OrderCrud_S2_Misaligned`

Run:

```bash
dotnet ef database update
dotnet run
```

The migration `AddPerformanceIndexes` is already in this branch (do not add it again).

Test:

- `GET /api/dashboard`
- `POST /api/benchmarks/run`

## 2b) Stage 2b - Dashboard-aligned indexes

**Story:** Indexes on columns the **dashboard query actually uses** (`OrderDate` for last-month revenue, `Status` for grouping, `OrderItems.ProductId` for top products). No extra FK-only indexes on `CustomerId` / `OrderId`, so **writes stay lighter** than 2a while reads improve more for this workload.

Branch:

```bash
git checkout step/02b-indexes-dashboard
```

Set connection string database name to:

- `OrderCrud_S2b_Dashboard`

Run:

```bash
dotnet ef database update
dotnet run
```

Test:

- `GET /api/dashboard`
- `POST /api/benchmarks/run`

Compare **2a vs 2b** with the same seed settings (`DemoSettings` / `appsettings`).

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
