# Крайні випадки: runner

Звіт за критерієм «обробка крайніх випадків і помилок». Усі випадки перевіряються автоматичними тестами в `tests/PerfCompare.Runner.Tests/RunnerEdgeCaseTests.cs` та в тестах, що вже були раніше (`ScenarioLoaderTests`, `CliOptionsTests`, `CliAppTests`, `CliOfflineTests`, `SeriesRunnerResilienceTests`). Запуск: `dotnet test tests/PerfCompare.Runner.Tests --filter "FullyQualifiedName~RunnerEdgeCaseTests"`.

Коди виходу: 0 успіх, 1 результат не відправлено (збережено у файл), 2 неправильна команда чи сценарій, 3 помилка виконання (файлова система чи інша помилка середовища), 130 серію перервано користувачем.

| № | Випадок | Що відбувається | Де оброблено | Тести | Оброблено |
|---|---|---|---|---|---|
| 1 | Валідація введення: порожні `name` і `command`, `repeats` 0, `timeoutSeconds` 0, `warmup` -1, порожній елемент у `args` | усі порушення перелічені в одному повідомленні, код 2, програма сценарію не запускається | `ScenarioValidator` | `Validation_AllFieldsInvalid_ReportsEveryField`, `Validation_NullArgument_IsReported`, `Parse_SeveralErrors_AreReportedTogether` | так |
| 2 | Порожні й некоректні дані: порожній файл, лише коментар, `[]`, звичайний текст, незакрита дужка, текст замість числа, число завеликого розміру, `args` не списком, двійкове сміття, папка замість файлу, відсутній файл, порожній `args:`, неправильна команда в командному рядку, відправка з порожньої папки, відповідь сервера не за контрактом | зрозуміле повідомлення і код 2 (для команди `send` з порожньої папки код 0 і текст «Немає файлів для відправки»); порожній `args:` вважається відсутністю аргументів; відповідь сервера не за контрактом вважається невдалою відправкою, результат зберігається у файл, код 1 | `ScenarioLoader`, `CliOptions`, `ResultClient`, `CliApp` | `MalformedScenario_ReturnsUsageCodeWithoutStackTrace`, `BinaryGarbageInsteadOfScenario_ReturnsUsageCode`, `DirectoryInsteadOfScenarioFile_ReturnsUsageCode`, `EmptyArgsList_IsTreatedAsNoArguments`, `WrongCommandLine_PrintsUsageAndReturnsUsageCode`, `SendFromEmptyFolder_ReportsNothingToSend`, `ServerAnswersWithGarbage_IsTreatedAsFailedSend` | так |
| 3 | Винятки без падіння: програми немає в системі, робоча папка не існує, шлях `--out` чи `--results-dir` неможливо створити, сервер недоступний, сервер не відповідає | відсутня програма й робоча папка дають вимірювання зі статусом `Failed`, серія триває, код 0; недоступний сервер дає три спроби, збереження результату у файл і код 1; неможливий шлях запису дає повідомлення «Помилка виконання» і код 3; жодного необробленого винятку | `ProcessExecutor`, `SeriesRunner`, `ResultClient`, `CliApp` | `UnknownProgram_IsRecordedAsFailedMeasurement`, `MissingWorkingDirectory_IsRecordedAsFailedMeasurement`, `UnwritableOutPath_ReturnsInternalCodeWithMessage`, `ServerDownAndUnwritableResultsDir_ReturnsInternalCode`, `Run_ServerDown_TriesThreeTimesThenSavesResult` | так |
| 4 | Межові значення: `repeats` 1 і 100, `timeoutSeconds` 1 і 3600, `warmup` 0, ім'я 100 символів; поза межами: 0, 101, 0, 3601, -1, ім'я 101 символ; таймаут в одну секунду для довгої програми | допустимі значення приймаються, недопустимі дають код 2; програма, що не вкладається в таймаут, зупиняється разом з усім деревом процесів і отримує статус `Timeout` | `ScenarioValidator`, `ProcessExecutor` | `BoundaryValues_AreAccepted`, `OutsideBoundary_IsRejected`, `ScenarioName_Of100CharactersIsAccepted_And101IsRejected`, `OneSecondTimeout_StopsLongProgramAndMarksTimeout` | так |
| 5 | Необроблені винятки в журналі запуску: після серії некоректних викликів у виводі немає слів «Unhandled exception» і рядків стеку виклику; запуск справжнього процесу з неіснуючим сценарієм завершується кодом 2 без звіту про аварію | усі очікувані помилки оброблені кодами 1, 2, 3; несподівані помилки файлової системи перехоплюються на верхньому рівні `CliApp` | `CliApp` | `ManyBadInvocations_LeaveNoStackTracesInOutput`, `RealProcess_WithMissingScenario_ExitsWithUsageCodeAndNoCrashReport` | так |

## Що змінено в коді

До цього випадки 3 і 5 мали три прогалини, які знайшов аналіз коду:

- запис результату в недоступний шлях (`--out`, `--results-dir`) завершував програму необробленим винятком файлової системи;
- відповідь сервера з кодом 201, але не у форматі JSON, завершувала програму винятком розбору JSON;
- порожній `args:` у YAML давав `null` замість списку, і запуск падав з `NullReferenceException`; читання файлу сценарію, який відкритий іншою програмою, давало необроблений виняток вводу-виводу.

Тепер кожна з них оброблена: `ScenarioLoader` перетворює помилки читання на `ScenarioException`, `ResultClient` вважає некоректну відповідь невдалою відправкою, `CliApp` перехоплює помилки файлової системи з кодом 3.

## Як відтворити вручну

З кореня репозиторію (після `dotnet build`):

- відсутній сценарій: `dotnet run --project src/PerfCompare.Runner -- run no-such.yaml`, очікується повідомлення про відсутній файл і код 2 (`$LASTEXITCODE` у PowerShell, `echo $?` у bash);
- недоступний шлях запису: `dotnet run --project src/PerfCompare.Runner -- run scenarios/baseline.yaml --out README.md/result.json`, очікується «Помилка виконання» і код 3;
- недоступний сервер: `dotnet run --project src/PerfCompare.Runner -- run scenarios/baseline.yaml --server http://localhost:59999`, очікуються три спроби, шлях збереженого файлу й код 1.
