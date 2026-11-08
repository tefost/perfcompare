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
| GET /api/runs/{id}/stats | статистика запуску | 200; 404 |
| GET /api/compare?scenarioId=&environmentIds= | порівняння середовищ | 200; 404; 422 |
| POST /api/baselines | позначити базову лінію | 201; 400; 422 |
| GET /api/regressions?runId=&threshold= | регресії відносно бази | 200; 404; 422 |
| GET /api/scenarios | список сценаріїв | 200 |
| GET /api/export.csv?scenarioId= | експорт у CSV | 200; 404; 422 |

Відповідь 422 має тіло `{"errors": ["поле: причина", ...]}`.

## Правила

- 400: тіло не розбирається як JSON. 422: JSON коректний, але дані порушують правила з таблиці.
- Зміни контракту вносяться лише через PR, який переглядає друга сторона.

## Деталі нових відповідей

`GET /api/runs/{id}/stats` повертає:

```json
{
  "total": 5, "passed": 5, "failed": 0, "timeouts": 0, "successRate": 1.0,
  "duration": { "min": 10, "max": 50, "mean": 30, "median": 30, "p95": 50, "stdDev": 15.81, "cvPercent": 52.7 },
  "meanPeakMemMb": 41.2, "meanCpuTimeMs": 790.0, "stabilityIndex": 47.3
}
```

- Час, пам'ять і процесорний час рахуються лише за вимірюваннями зі статусом `Passed`. Якщо жодного немає, `duration` дорівнює null, `stabilityIndex` дорівнює 0.
- `meanPeakMemMb` і `meanCpuTimeMs` дорівнюють null, якщо жодного значення не отримано.
- `p95` береться за найближчим рангом, `stdDev` вибіркове (ділення на n - 1).
- `stabilityIndex` = max(0, 100 - cvPercent) помножене на `successRate`, округлене до десятих.

`POST /api/baselines` приймає `{"runId": N}` і повертає 201 `{"baselineId": N}`. Базова лінія одна на пару сценарій-середовище, повторний виклик замінює запуск і повертає той самий `baselineId`. Якщо запуску немає, відповідь 422 з `errors`.

`GET /api/compare?scenarioId=1&environmentIds=1,2` повертає:

```json
{
  "scenarioId": 1, "scenario": "baseline",
  "items": [
    { "environmentId": 1, "environment": { "os": "Windows", "osVersion": "10.0.22631", "runtime": ".NET 9.0.2", "cores": 8, "ramMb": 16384 },
      "runId": 3, "startedAt": "2026-11-07T10:15:00Z", "baselineRunId": 1, "stats": { }, "slowdownVsFastest": 1.0 }
  ]
}
```

- Для кожного середовища береться останній запуск сценарію. Параметр `environmentIds` необов'язковий, без нього повертаються всі середовища.
- `slowdownVsFastest` - відношення медіани часу до найменшої медіани серед елементів (1.0 для найшвидшого), null, якщо успішних вимірювань немає.
- 404, якщо сценарію немає; 422, якщо `scenarioId` відсутній чи не є додатним числом або `environmentIds` містить не числа.

## Регресії

`GET /api/regressions?runId=3&threshold=10` порівнює запуск із базовою лінією **того самого сценарію в тому самому середовищі** і повертає:

```json
{
  "runId": 3, "baselineRunId": 1, "status": "regression", "thresholdPercent": 10,
  "metrics": [
    { "metric": "durationMedianMs", "baseline": 100, "current": 130, "changePercent": 30, "isRegression": true }
  ]
}
```

- `runId` обов'язковий, додатне число. `threshold` необов'язковий, відсоток від 0 (не включно) до 1000, за замовчуванням 10. Некоректні параметри дають 422 з `errors`, невідомий запуск дає 404.
- `status`: `ok`, якщо жодна метрика не погіршилась більше ніж на поріг; `regression`, якщо хоча б одна погіршилась; `no_baseline`, якщо для пари сценарій-середовище базову лінію не позначено (тоді `baselineRunId` дорівнює null, `metrics` порожній, відповідь 200).
- Метрики: `durationMedianMs`, `durationP95Ms`, `meanPeakMemMb`, `meanCpuTimeMs` (погіршення - зростання) і `successRate` (погіршення - зменшення). `changePercent` = (поточне - базове) / базове * 100, округлене до сотих.
- Погіршенням вважається зміна, строго більша за поріг: рівно на порозі регресії немає.
- Метрика пропускається, якщо її немає в одному із запусків (наприклад, пам'ять недоступна) або базове значення дорівнює нулю.


## Список сценаріїв, експорт і дашборд

`GET /api/scenarios` повертає масив `[{ "id": 1, "name": "baseline" }]`, упорядкований за `id`. Порожня база дає `[]`.

`GET /api/export.csv?scenarioId=1` віддає CSV з усіма вимірюваннями всіх запусків сценарію (кодування UTF-8 з BOM, розділювач рядків CRLF). Колонки: `runId, scenario, environmentId, os, osVersion, runtime, cores, ramMb, startedAt, iteration, durationMs, peakMemMb, cpuTimeMs, exitCode, status`. Відсутні значення записуються порожніми. Текстові поля, що починаються з `=`, `+`, `-` або `@`, отримують на початку апостроф, щоб електронна таблиця не сприйняла їх як формулу. `scenarioId` обов'язковий, додатне число (інакше 422), невідомий сценарій дає 404. Відповідь має заголовок `Content-Disposition: attachment`.

Дашборд доступний за адресою `/` і працює лише через описані вище ендпоінти. Сторінка і бібліотека графіків вбудовані в збірку сервера, окремих файлів і доступу до інтернету не потрібно.

