# Крайні випадки: server

Звіт за критерієм «обробка крайніх випадків і помилок». Усі випадки перевіряються автоматичними тестами в `tests/PerfCompare.Server.Tests/EdgeCaseTests.cs`. Запуск: `dotnet test tests/PerfCompare.Server.Tests --filter "FullyQualifiedName~EdgeCaseTests"`.

| № | Випадок | Що відбувається | Де оброблено | Тести | Оброблено |
|---|---|---|---|---|---|
| 1 | Валідація введення: запуск порушує правила контракту (`repeats` 0, порожнє ім'я, `cores` 0) | 422 з переліком усіх знайдених помилок, у базу нічого не записується | `RunValidator`, `RunsEndpoints` | `Validation_InvalidRun_Returns422ListingEveryProblem`, `RunValidatorTests` | так |
| 2 | Порожні й некоректні дані: порожнє тіло, `null`, `[]`, обірваний JSON, текст чи число замість об'єкта, неправильний тип поля, `{}`, сміття в параметрах запиту | зламаний JSON дає 400, JSON без обов'язкових полів і неправильні параметри дають 422, невідомий шлях дає 404; відповідь завжди з описом помилки | `RequestBody`, `RunValidator`, перевірки параметрів у `ComparisonEndpoints`, `RegressionEndpoints`, `ExportEndpoints` | `MalformedBody_Returns400`, `EmptyObject_Returns422WithErrors`, `GarbageQuery_Returns422`, `UnknownPath_Returns404` | так |
| 3 | Виняток під час обробки запиту (наприклад, файл бази зник або пошкоджений) | сервер не завершує роботу: відповідь 500 з тілом `{"errors":["внутрішня помилка сервера"]}` без технічних подробиць, наступні запити обслуговуються | `ErrorHandling.UseJsonErrors` | `DatabaseFailure_Returns500JsonWithoutDetails_AndServerKeepsRunning` | так |
| 4 | Межові значення: `repeats` 1 і 100, `timeoutSeconds` 1 і 3600, ім'я 100 символів, `cores` 1, `durationMs` 0, кількість вимірювань рівно `repeats` | приймаються (201); значення 0, 101, 0, 3601, ім'я 101 символ, від'ємний час, вимірювань більше за `repeats` відхиляються (422) | `RunValidator` | `BoundaryValues_AreAccepted`, `BoundaryValues_LongestNameZeroDurationAndOneCore_AreAccepted`, `OutsideBoundary_IsRejected`, `OutsideBoundary_NameOf101CharactersAndNegativeDuration_AreRejected` | так |
| 5 | Необроблені винятки в журналі: після серії некоректних запитів у журналі немає записів рівня Error і винятків; справжня серверна помилка потрапляє в журнал як оброблена | усі помилки клієнта обробляються кодами 400, 404, 422 без винятків; серверна помилка логується один раз з винятком і повертає 500 | `ErrorHandling`, весь код ендпоінтів | `BadRequests_LeaveNoErrorsInLog`, `NormalWork_LeavesNoErrorsInLog`, `DatabaseFailure_IsLoggedAsError` | так |

## Як відтворити вручну

Запустіть сервер (`dotnet run --project src/PerfCompare.Server --urls http://localhost:5000`) і виконайте в другому вікні:

- обірваний JSON: `curl.exe -i -X POST http://localhost:5000/api/runs -H "Content-Type: application/json" -d "{ not json"` - очікується `400 Bad Request`;
- порожній об'єкт: `curl.exe -i -X POST http://localhost:5000/api/runs -H "Content-Type: application/json" -d "{}"` - очікується `422` зі списком помилок;
- некоректний параметр: `curl.exe -i "http://localhost:5000/api/regressions?runId=-1"` - очікується `422`;
- невідомий запуск: `curl.exe -i http://localhost:5000/api/runs/999` - очікується `404`.
