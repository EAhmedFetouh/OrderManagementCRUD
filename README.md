# OrderManagement.CRUD (demo by branch)

مشروع تجريبي للأداء: كل **فرع (branch)** يمثل مرحلة مختلفة. انسخ المستودع ثم بدّل الفرع حسب ما تريد تجربته.

## المتطلبات

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- SQL Server (محلي أو Docker) + تعديل `ConnectionStrings:DefaultConnection` في `appsettings.json`
- لكل مرحلة يُفضّل **اسم قاعدة بيانات منفصل** (انظر `STAGES_RUNBOOK.md`)

## استنساخ والتبديل بين الفروع

```bash
git clone <رابط-المستودع>
cd OrderManagement.CRUD

git branch -a
git checkout step/01-crud-baseline
```

## أسماء الفروع (بالترتيب التقريبي)

| الفرع | الفكرة |
|--------|--------|
| `step/01-crud-baseline` | Baseline |
| `step/02-indexes-misaligned` | فهارس غير متوافقة |
| `step/02b-indexes-dashboard` | فهارس مناسبة للداشبورد |
| `step/03-add-caching` | كاش للداشبورد |
| `step/04-add-materialized-view` | Materialized / indexed view |

## تشغيل بعد اختيار الفرع

```bash
dotnet ef database update
dotnet run
```

التفاصيل الكاملة (أسماء الـ DB المقترحة، الاختبارات): **`STAGES_RUNBOOK.md`**

## رفع المستودع (لصاحب المشروع)

```bash
git remote add origin <رابط-المستودع-الفارغ>
git push -u origin --all
```
