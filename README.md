# PerfCompare

Платформа порівняння продуктивності та стабільності програмного застосунку на різних конфігураціях середовища. Runner запускає застосунок за сценарієм, вимірює час виконання, піковий обсяг пам'яті та процесорний час і надсилає результати на сервер. Сервер зберігає їх у SQLite, порівнює середовища, шукає регресії відносно базової лінії та показує дашборд.

## Хто за що відповідав

| Учасник | Зона | Відповідальність | Задачі |
|---|---|---|---|
| Артем (tefost) | Runner, тестові застосунки, CI, релізи | Завантаження і валідація YAML-сценаріїв, запуск процесів із таймаутом, збір метрик і відбитка середовища, CLI, відправка результатів на сервер, збереження у файл при недоступному сервері, GitHub Actions на трьох ОС, релізні збірки | [issues](https://github.com/tefost/perfcompare/issues?q=label%3Arunner), [PR](https://github.com/tefost/perfcompare/pulls?q=author%3Atefost) |
| Саша (S4NYAKABAN) | Server, база даних, аналітика, дашборд | Контракт обміну даними, схема SQLite, шар доступу до даних, REST API, статистика, порівняння середовищ, регресії, дашборд, експорт | [issues](https://github.com/tefost/perfcompare/issues?q=label%3Aserver), [PR](https://github.com/tefost/perfcompare/pulls?q=author%3AS4NYAKABAN) |

## Вимоги

- Windows, Linux або macOS
- .NET 9 SDK для запуску з вихідного коду (для готових збірок не потрібен)

## Запуск з вихідного коду

```bash
git clone https://github.com/tefost/perfcompare.git
cd perfcompare
dotnet build
dotnet test
```

Сервер (дашборд відкривається на http://localhost:5000):

```bash
dotnet run --project src/PerfCompare.Server --urls http://localhost:5000
```

Runner в іншому терміналі:

```bash
dotnet run --project src/PerfCompare.Runner -- run scenarios/baseline.yaml --server http://localhost:5000
```

## Запуск із готових збірок

Архіви лежать у розділі Releases: `perfcompare-win-x64.zip`, `perfcompare-linux-x64.tar.gz`, `perfcompare-osx-arm64.tar.gz`. У кожному є `PerfCompare.Server`, `PerfCompare.Runner` і папка `scenarios`.

Windows (PowerShell):

```powershell
Expand-Archive perfcompare-win-x64.zip -DestinationPath perfcompare
cd perfcompare
.\PerfCompare.Server.exe --urls http://localhost:5000
```

Runner в іншому вікні PowerShell:

```powershell
.\PerfCompare.Runner.exe run scenarios\baseline.yaml --server http://localhost:5000
```

Linux:

```bash
mkdir perfcompare && tar -xzf perfcompare-linux-x64.tar.gz -C perfcompare
cd perfcompare
chmod +x PerfCompare.Server PerfCompare.Runner
./PerfCompare.Server --urls http://localhost:5000
```

Runner в іншому терміналі: `./PerfCompare.Runner run scenarios/baseline.yaml --server http://localhost:5000`.

macOS: ті самі команди з архівом `perfcompare-osx-arm64.tar.gz`. Якщо система блокує запуск, зніміть карантин командою `xattr -d com.apple.quarantine PerfCompare.Server PerfCompare.Runner`.

## Формат сценарію

```yaml
name: baseline
command: dotnet
args: ["--version"]
repeats: 5
timeoutSeconds: 30
warmup: 1
```

Поля: `name` (до 100 символів), `command`, `args` (список), `repeats` (1-100), `timeoutSeconds` (1-3600), `warmup` (не менше 0), `workingDirectory` (необов'язково). Невідомі ключі відхиляються.

## Документація

- `docs/contract.md`: контракт між runner і server
- `docs/decisions.md`: журнал рішень
