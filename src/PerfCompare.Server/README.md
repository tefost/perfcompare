# PerfCompare.Server

Сервер приймає результати запусків від runner, зберігає їх у SQLite, рахує статистику, порівняння середовищ і регресії та віддає дашборд, CSV і HTML-звіт. Архітектура описана в [docs/architecture.md](../../docs/architecture.md).

## Запуск

```bash
dotnet run --project src/PerfCompare.Server --urls http://localhost:5000
```

Дашборд відкривається за адресою `http://localhost:5000/`. Файл бази за замовчуванням `perfcompare.db` створюється в поточній папці. Інше розташування задають налаштуванням `Database:Path`:

```bash
dotnet run --project src/PerfCompare.Server --urls http://localhost:5000 --Database:Path=/tmp/perf.db
```

Окремих залежностей, мережі й попередньо створеної бази не потрібно: схема застосовується при старті.

## Ендпоінти

| Метод і шлях | Призначення |
|---|---|
| `POST /api/runs` | зберегти результат запуску (201, 400, 422) |
| `GET /api/runs` | список запусків |
| `GET /api/runs/{id}` | один запуск з вимірюваннями (404, якщо немає) |
| `GET /api/runs/{id}/stats` | статистика запуску |
| `POST /api/baselines` | позначити запуск базовим |
| `GET /api/compare?scenarioId=` | порівняння середовищ для сценарію |
| `GET /api/regressions?runId=&threshold=` | перевірка регресії відносно базової лінії |
| `GET /api/scenarios` | список сценаріїв |
| `GET /api/export.csv?scenarioId=` | CSV з усіма вимірюваннями сценарію |
| `GET /api/report.html?scenarioId=&threshold=` | HTML-звіт по сценарію |
| `GET /` | дашборд |

Повний опис полів, правил і кодів відповіді в [docs/contract.md](../../docs/contract.md).

## Приклад

```bash
curl -i -X POST http://localhost:5000/api/runs -H "Content-Type: application/json" -d '{"scenario":{"name":"demo","command":"samples/CpuBound","repeats":1,"timeoutSeconds":30},"environment":{"os":"Linux","osVersion":"6.1","runtime":".NET 9.0","cores":4,"ramMb":8192},"startedAt":"2026-11-09T10:00:00Z","measurements":[{"iteration":1,"durationMs":120.5,"peakMemMb":30.1,"cpuTimeMs":110.0,"exitCode":0,"status":"Passed"}]}'
```

Очікується `201 Created` і `{"runId":1}`.

## Тести і покриття

```bash
dotnet test tests/PerfCompare.Server.Tests
./scripts/coverage.ps1 -Name Server
```

Другий рядок запускає тести з вимірюванням покриття, будує звіт у `docs/reports/coverage-server.md` і завершується помилкою, якщо покриття рядків нижче 80%. Опис в [docs/reports/README.md](../../docs/reports/README.md).
