# Покриття тестами: Server

Дата: 2026-11-08. Засоби: coverlet.collector (вимірювання), ReportGenerator (звіт). Збірка тестів: tests/PerfCompare.Server.Tests.

Команда для відтворення: `./scripts/coverage.ps1 -Name Server`.

| Показник | Значення |
|---|---|
| Покриття рядків | 98.5% |
| Покриття гілок | 94.4% |
| Поріг для рядків | 80% |
| Висновок | поріг виконано |

## Звіт ReportGenerator

# Summary
<details open><summary>Summary</summary>

|||
|:---|:---|
| Generated on: | 08.11.2026 - 21:21:59 |
| Coverage date: | 08.11.2026 - 21:21:56 |
| Parser: | Cobertura |
| Assemblies: | 1 |
| Classes: | 39 |
| Files: | 32 |
| **Line coverage:** | 98.5% (1015 of 1030) |
| Covered lines: | 1015 |
| Uncovered lines: | 15 |
| Coverable lines: | 1030 |
| Total lines: | 1678 |
| **Branch coverage:** | 94.4% (291 of 308) |
| Covered branches: | 291 |
| Total branches: | 308 |
| **Method coverage:** | [Feature is only available for sponsors](https://reportgenerator.io/pro) |

</details>

## Coverage
<details><summary>PerfCompare.Server - 98.5%</summary>

|**Name**|**Line**|**Branch**|
|:---|---:|---:|
|**PerfCompare.Server**|**98.5%**|**94.4%**|
|PerfCompare.Server.Analytics.BaselineCreated|100%||
|PerfCompare.Server.Analytics.BaselineRequest|100%||
|PerfCompare.Server.Analytics.ComparisonItem|100%||
|PerfCompare.Server.Analytics.ComparisonResult|100%||
|PerfCompare.Server.Analytics.ComparisonService|91.3%|77.7%|
|PerfCompare.Server.Analytics.DurationStats|100%||
|PerfCompare.Server.Analytics.MetricChange|100%||
|PerfCompare.Server.Analytics.RegressionDetector|100%|88.8%|
|PerfCompare.Server.Analytics.RegressionReport|100%||
|PerfCompare.Server.Analytics.RegressionService|100%|100%|
|PerfCompare.Server.Analytics.RunStats|100%||
|PerfCompare.Server.Analytics.RunStatsCalculator|100%|87.5%|
|PerfCompare.Server.Analytics.Statistics|100%|100%|
|PerfCompare.Server.Api.ApiRoutes|100%|100%|
|PerfCompare.Server.Api.ComparisonEndpoints|100%|100%|
|PerfCompare.Server.Api.CsvWriter|100%|100%|
|PerfCompare.Server.Api.DashboardEndpoints|87.1%|71.4%|
|PerfCompare.Server.Api.ErrorHandling|100%|100%|
|PerfCompare.Server.Api.ErrorsResponse|100%||
|PerfCompare.Server.Api.ExportEndpoints|95.9%|93.7%|
|PerfCompare.Server.Api.HtmlReportBuilder|98.5%|93.7%|
|PerfCompare.Server.Api.RegressionEndpoints|100%|100%|
|PerfCompare.Server.Api.ReportEndpoints|100%|100%|
|PerfCompare.Server.Api.RequestBody|100%||
|PerfCompare.Server.Api.RunsEndpoints|100%|100%|
|PerfCompare.Server.Api.RunValidator|97.2%|98.1%|
|PerfCompare.Server.Api.ScenariosEndpoints|100%||
|PerfCompare.Server.Api.StatsEndpoints|100%|100%|
|PerfCompare.Server.Database.BaselineRepository|100%|93.7%|
|PerfCompare.Server.Database.DatabaseInitializer|100%|50%|
|PerfCompare.Server.Database.EnvironmentLatestRun|100%||
|PerfCompare.Server.Database.RunKey|100%||
|PerfCompare.Server.Database.RunKeyReader|100%|100%|
|PerfCompare.Server.Database.RunRepository|100%|100%|
|PerfCompare.Server.Database.RunSummary|100%||
|PerfCompare.Server.Database.ScenarioItem|100%||
|PerfCompare.Server.Database.ScenarioRepository|100%|100%|
|PerfCompare.Server.Database.SqliteConnectionFactory|100%||
|Program|100%|50%|

</details>

