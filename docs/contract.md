# Контракт runner - server

Формат обміну: JSON, ключі camelCase, enum рядком, дати UTC у форматі ISO 8601.

## Структура результату запуску

```json
{
  "scenario": { "name": "baseline", "command": "samples/CpuBound", "repeats": 5, "timeoutSeconds": 30 },
  "environment": { "os": "Windows", "osVersion": "10.0.22631", "runtime": ".NET 9.0.2", "cores": 8, "ramMb": 16384 },
  "startedAt": "2026-11-07T10:15:00Z",
  "measurements": [
    { "iteration": 1, "durationMs": 812.4, "peakMemMb": 41.2, "cpuTimeMs": 790.0, "exitCode": 0, "status": "Passed" }
  ]
}
```

## Поля

| Поле | Тип | Правило |
|---|---|---|
| scenario.name | string | непорожній, до 100 символів |
| scenario.command | string | непорожній |
| scenario.repeats | int | від 1 до 100 |
| scenario.timeoutSeconds | int | від 1 до 3600 |
| environment.os | string | непорожній |
| environment.osVersion | string | непорожній |
| environment.runtime | string | непорожній |
| environment.cores | int | не менше 1 |
| environment.ramMb | long | не менше 1 |
| startedAt | дата-час | UTC |
| measurements | масив | не порожній, кількість не більша за repeats |
| measurements[].iteration | int | починається з 1, унікальний у запуску |
| measurements[].durationMs | number | не менше 0 |
| measurements[].peakMemMb | number або null | null, якщо метрика недоступна |
| measurements[].cpuTimeMs | number або null | null, якщо метрика недоступна |
| measurements[].exitCode | int або null | null при таймауті |
| measurements[].status | string | Passed, Failed або Timeout |

## Ендпоінти

| Метод і шлях | Призначення | Відповіді |
|---|---|---|
| POST /api/runs | прийняти запуск | 201 `{"runId": N}`; 400 некоректний JSON; 422 не пройшла валідація |
| GET /api/runs | список запусків | 200 |
| GET /api/runs/{id} | один запуск | 200; 404 |
| GET /api/compare?scenarioId=&environmentIds= | порівняння середовищ | 200; 404 |
| POST /api/baselines | позначити базову лінію | 201; 422 |
| GET /api/regressions?runId=&threshold= | регресії відносно бази | 200; 404 |
| GET /api/export.csv?scenarioId= | експорт у CSV | 200 |

Відповідь 422 має тіло `{"errors": ["поле: причина", ...]}`.

## Правила

- 400: тіло не розбирається як JSON. 422: JSON коректний, але дані порушують правила з таблиці.
- Зміни контракту вносяться лише через PR, який переглядає друга сторона.
