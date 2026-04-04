# OrderManagement.CRUD

Performance-oriented demo API. **Each Git branch is a different stage** of the same app (indexes, caching, materialized view). Clone once, then `git checkout` the branch you want to run.

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [SQL Server](https://www.microsoft.com/sql-server) (local install or Docker)
- [EF Core CLI](https://learn.microsoft.com/en-us/ef/core/cli/dotnet) (for migrations):  
  `dotnet tool install --global dotnet-ef` (if you do not have it)

## Configuration

1. Open `appsettings.json`.
2. Set `ConnectionStrings:DefaultConnection` to your SQL Server instance.
3. **Use a different database name per stage** so schemas and seeds do not clash (recommended names are in `STAGES_RUNBOOK.md`).

Example:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=OrderCrud_S1_Baseline;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
}
```

## Clone and switch branches

```bash
git clone https://github.com/YOUR_USER/YOUR_REPO.git
cd OrderManagement.CRUD

git branch -a
git checkout step/01-crud-baseline
```

List of stage branches (rough order):

| Branch | What it demonstrates |
|--------|----------------------|
| `step/01-crud-baseline` | Baseline CRUD + heavy dashboard query |
| `step/02-indexes-misaligned` | Indexes that do not match the hot query pattern |
| `step/02b-indexes-dashboard` | Indexes aligned with the dashboard query |
| `step/03-add-caching` | In-memory caching for the dashboard |
| `step/04-add-materialized-view` | SQL Server indexed view (materialized aggregation) |

## Run the app (after choosing a branch)

From the project folder (`OrderManagement.CRUD`):

```bash
dotnet restore
dotnet ef database update
dotnet run
```

- **First run** may seed a large dataset (see `DemoSettings` in `appsettings.json`). Reduce `SeedOrders` for a quicker setup.
- Swagger (if enabled for Development): open the URL shown in the console (often `https://localhost:7xxx` or `http://localhost:5xxx`).

## Useful HTTP endpoints (typical)

- `GET /api/dashboard` — dashboard aggregation  
- `POST /api/benchmarks/run` — load test endpoint (if present on that branch)  
- On later branches: `GET /api/dashboard/compare`, materialized benchmarks, etc. (see `STAGES_RUNBOOK.md`)

## More detail

Full per-stage database names, migration commands, and test checklist: **`STAGES_RUNBOOK.md`**.

## Publish all branches (maintainers)

```bash
git remote add origin https://github.com/YOUR_USER/YOUR_REPO.git
git push -u origin --all
```

Replace the URL with your empty repository. Participants then clone and `git checkout` any `step/...` branch as above.
