# Звіти якості

Файли в цій папці створюють скрипти з `scripts/`, руками їх не редагують. Після зміни коду звіти оновлюють повторним запуском скрипта.

| Файл | Що містить | Як створено |
|---|---|---|
| `coverage-server.md` | покриття рядків і гілок сервера, таблиця за класами | `./scripts/coverage.ps1 -Name Server` |
| `coverage-runner.md` | те саме для runner | `./scripts/coverage.ps1 -Name Runner` |
| `analyzer-report.md` | попередження SonarAnalyzer на 1000 рядків коду та їх перелік | `./scripts/analyzer-report.ps1` |

## Засоби

- coverlet.collector вимірює покриття під час `dotnet test` (пакет підключений у тестових проєктах);
- ReportGenerator перетворює файл `coverage.cobertura.xml` у зведення; версія зафіксована в `.config/dotnet-tools.json`, відновлюється командою `dotnet tool restore`;
- SonarAnalyzer.CSharp підключений для всіх проєктів у `Directory.Build.props` і працює під час збірки.

## Поріг і CI

Скрипт покриття завершується помилкою, якщо покриття рядків нижче 80%. Робочий процес `.github/workflows/quality.yml` запускає його для сервера на кожному Pull Request і завантажує звіти як артефакти. Звіт аналізаторів у CI лише збирається, збірка не падає від попереджень.

## Повторити локально

```powershell
dotnet tool restore
./scripts/coverage.ps1 -Name Server
./scripts/analyzer-report.ps1
```
