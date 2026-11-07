# OrderManagement.CRUD

كود الحلقة 4 من كورس CQRS: قياس أداء تطبيق Order Management قبل CQRS. كل branch يمثل مرحلة.

| Branch | المرحلة |
|---|---|
| `step/01-crud-baseline` | Baseline من غير Indexes إضافية |
| `step/02-indexes-misaligned` | Indexes مش مناسبة لاستعلام الـ Dashboard (مش معروضة في الفيديو) |
| `step/02b-indexes-dashboard` | Indexes مناسبة للـ Dashboard |
| `step/03-add-caching` | In-memory cache لمدة 30 ثانية |
| `step/04-add-materialized-view` | Indexed Views في SQL Server |

> الـ branches مش تراكمية: `step/03` و`step/04` مفيهمش Indexes الخاصة بـ `step/02b`.

## المتطلبات

- .NET 8 SDK
- SQL Server محلي أو على Docker
- `dotnet tool install --global dotnet-ef` (اختياري، لأن التطبيق بيعمل migrate عند التشغيل)

## التشغيل

1. عدّل `ConnectionStrings:DefaultConnection` في `appsettings.json`. على Mac أو Linux استخدم `User Id` و`Password` بدل `Trusted_Connection`.
2. استخدم اسم قاعدة بيانات مختلف لكل branch، مثل `OrderCrud_S1_Baseline`.
3. شغّل:

```bash
dotnet run
```

أول تشغيل بيعمل seed لحوالي 100 ألف Order و2 مليون Order Item، وده بياخد وقت. لتجربة أسرع قلّل `DemoSettings:SeedOrders`.

## القياس

```text
GET  /api/dashboard
POST /api/benchmarks/run
```

- شغّل الـ Benchmark 3 مرات وخد الوسيط.
- الأرقام بتختلف من جهاز للتاني، فقارن الاتجاه مش الرقم نفسه.
- الـ Benchmark بيتنفذ جوه التطبيق من غير HTTP.

### `step/03-add-caching`

اطلب `GET /api/dashboard` الأول، وبعدها شغّل الـ Benchmark خلال أقل من 30 ثانية. لو الكاش فاضي، الـ 50 قراءة كلهم هيروحوا لقاعدة البيانات في نفس اللحظة (Cache Stampede).

### `step/04-add-materialized-view`

```text
GET  /api/dashboard/compare
POST /api/benchmarks/materialized-impact?rawDashboardMs=...&rawCreateOrderMs=...&rawMixedWriteMs=...
```

القيم في الطلب التاني هي أرقامك من `step/01-crud-baseline`.
