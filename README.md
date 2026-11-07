# Order Management: Measuring Performance Before CQRS

Companion code for **Part 4 of the CQRS course**.

This is a small .NET 8 and EF Core order API with about 100,000 orders and 1.75 million order items. Before introducing CQRS, the video measures what happens when 50 requests hit the app at the same time, then tries three common fixes: indexes, caching, and an indexed view.

**Each branch is one stage.** Clone the repository once, then switch branches to run a different stage.

## Stages

| Branch | What it adds | In the video? |
|---|---|---|
| `step/01-crud-baseline` | Starting point with no secondary indexes | Yes, measured |
| `step/02-indexes-misaligned` | Indexes that do not match the dashboard query | No, extra comparison |
| `step/02b-indexes-dashboard` | Indexes on `OrderDate`, `Status`, `ProductId`, and `OrderId` | Yes, measured |
| `step/03-add-caching` | In-memory dashboard cache with a 30-second lifetime | Yes, measured |
| `step/04-add-materialized-view` | SQL Server indexed views for the dashboard | Code only, this is your task |

> **Branches are not cumulative.** `step/03` and `step/04` start from the baseline and do not include the indexes from `step/02b`.

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- SQL Server, installed locally or running in Docker

## Getting started

### 1. Clone and choose a stage

```bash
git clone https://github.com/EAhmedFetouh/OrderManagementCRUD.git
cd OrderManagementCRUD
git checkout step/01-crud-baseline
```

### 2. Connect to SQL Server

**Windows with a local SQL Server:** the default connection string in `appsettings.json` works as is.

**Docker on Windows, macOS, or Linux:**

```bash
docker run -d --name orders-sql \
  -e ACCEPT_EULA=Y \
  -e MSSQL_SA_PASSWORD='Your_Strong_Passw0rd' \
  -p 1433:1433 \
  mcr.microsoft.com/mssql/server:2022-latest
```

On Apple Silicon, add `--platform linux/amd64` to the command above.

Then set the connection string in the same terminal you will run the app from:

```bash
export ConnectionStrings__DefaultConnection="Server=localhost,1433;Database=OrderCrud_S1_Baseline;User Id=sa;Password=Your_Strong_Passw0rd;TrustServerCertificate=True;MultipleActiveResultSets=true"
```

Use a **different database name for each branch**, such as `OrderCrud_S1_Baseline` and `OrderCrud_S2_Indexes`, so their schemas do not clash.

### 3. Run the app

```bash
dotnet run
```

The first run creates the database and seeds it, so it can take a few minutes. For a quicker try, lower `DemoSettings:SeedOrders` in `appsettings.json`. Fewer rows will give you much smaller timings than the video.

The app listens on `http://localhost:5186`, and Swagger is at `http://localhost:5186/swagger`.

## Running the benchmark

```bash
curl -X POST http://localhost:5186/api/benchmarks/run
```

Each call runs three tests inside the app, without going through HTTP:

| Test | Load |
|---|---|
| Writes only | 50 concurrent `CreateOrder` calls |
| Reads only | 50 concurrent dashboard reads |
| Mixed | 25 writes and 25 reads at the same time |

All times in the response are **average milliseconds per operation**:

| Field | Meaning |
|---|---|
| `concurrentWritesOnlyMs` | Average time per write in the writes-only test |
| `concurrentReadsOnlyMs` | Average time per dashboard read in the reads-only test |
| `mixedLoad.avgCreateOrderTimeMs` | Average write time under mixed load |
| `mixedLoad.avgGetDashboardTimeMs` | Average dashboard read time under mixed load |
| `mixedLoad.degradationPercent` | How much slower mixed writes were than writes only |

Run it **three times** and take the median. A single run can be noisy.

### Testing the cache (`step/03-add-caching`)

Warm the cache first, then run the benchmark within 30 seconds:

```bash
curl http://localhost:5186/api/dashboard
curl -X POST http://localhost:5186/api/benchmarks/run
```

If the cache is empty, all 50 reads go to the database at once. This is called a **cache stampede**, and it makes the read result look like the uncached numbers.

### Testing the indexed views (`step/04-add-materialized-view`)

Compare both dashboard queries over the same last-month window:

```bash
curl http://localhost:5186/api/dashboard/compare
```

Compare against your own baseline results by passing your numbers from `step/01-crud-baseline`:

```bash
curl -X POST "http://localhost:5186/api/benchmarks/materialized-impact?rawDashboardMs=50500&rawCreateOrderMs=58&rawMixedWriteMs=204"
```

> **This code has moved on from the video.** The views are now grouped by day so they can answer the same last-month question as the raw query, and top products also come from a view. The original view is in the `AddDashboardMaterializedView` migration, and the update is in `ScopeDashboardViewsByDay`.

## Results from the video

Median of three runs:

| Stage | Write alone (50) | Read alone (50) | Write mixed (25+25) | Read mixed (25+25) |
|---|---|---|---|---|
| Baseline | 58 ms | 50.5 s | 204 ms | 29.8 s |
| Indexes | 69 ms | 48.3 s | 178 ms | 26.8 s |
| Caching | 12.65 ms | 0.0005 ms | 12.37 ms | 0.0006 ms |

How to read these numbers:

- They come from one developer machine in Debug mode. **Compare the trend, not the exact values.** They do not represent production.
- Request counts differ between the two test types, so compare down a column, not across columns.
- The index difference in reads is smaller than the variation between runs, so it does not prove a clear improvement.
- `0.0005 ms` is an in-app cache hit with a warm cache, which is the best case. The same request through Postman took about 30 ms.
- The drop in writes-only time during the caching stage was not caused by the cache, because that test runs no reads. Its cause has not been measured.

## Your task

Measure `step/04-add-materialized-view` with the same benchmark, then share your results in the video comments.

## Troubleshooting

| Problem | What to do |
|---|---|
| Login or connection errors on macOS or Linux | `Trusted_Connection` is Windows only. Use the Docker connection string above. |
| Seed is skipped | The database already has orders. Use a new database name or delete the old database. |
| Odd schema errors after switching branches | Each branch needs its own database name. |
| Cached reads are as slow as uncached reads | The cache was empty or older than 30 seconds. Warm it again, then run the benchmark. |
| `materialized-impact` returns 400 | All three `raw...` query parameters are required. |
