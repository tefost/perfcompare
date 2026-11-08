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

## Приклади застосунків

У папці `samples/` лежать три невеликі застосунки, на яких зручно перевіряти вимірювання:

| Застосунок | Що робить | Що показує |
|---|---|---|
| `CpuBound` | рахує прості числа до заданої межі | навантаження на процесор |
| `SortFile` | генерує, записує у файл, читає і сортує числа | пам'ять і диск |
| `Flaky` | у частині запусків завершується з помилкою | статус `Failed` і стабільність |

З репозиторію їх спершу треба зібрати (потрібен .NET 9 SDK):

```bash
dotnet publish samples/CpuBound -c Release -o samples/bin
dotnet publish samples/SortFile -c Release -o samples/bin
dotnet publish samples/Flaky -c Release -o samples/bin
```

У релізних архівах приклади вже зібрані в `samples/bin` і не потребують встановленого .NET.

У папці `scenarios/` лежить готовий набір сценаріїв:

| Файл | Призначення |
|---|---|
| `baseline.yaml` | базове вимірювання `CpuBound`, 5 повторів, 1 прогрівний |
| `slow.yaml` | той самий сценарій `baseline` із важчим навантаженням, дає регресію відносно базової лінії |
| `flaky.yaml` | `Flaky` із 30% збоїв, 10 повторів |
| `timeout.yaml` | завдання, яке не вкладається в 2 секунди, дає статус `Timeout` |
| `sort.yaml` | `SortFile`, 5 повторів |

У полі `command` можна вказати шлях до програми через `/`, наприклад `samples/bin/CpuBound`. Шлях рахується від поточної папки, на Windows розширення `.exe` додається автоматично, тож один і той самий сценарій працює на всіх ОС. Звичайні команди на кшталт `dotnet` шукаються у `PATH`.

Приклад повного циклу з репозиторію:

```bash
dotnet run --project src/PerfCompare.Runner -- run scenarios/baseline.yaml --server http://localhost:5000
dotnet run --project src/PerfCompare.Runner -- run scenarios/slow.yaml --server http://localhost:5000
```

## Серія вимірювань: прогрів, переривання, збереження результату

- Поле `warmup` задає кількість прогрівних запусків. Вони виконуються перед серією, показуються в консолі як `[прогрів 1/1]`, але в результат не потрапляють.
- Якщо одна ітерація впала або не змогла стартувати, вона записується зі статусом `Failed`, а серія триває далі.
- `Ctrl+C` завершує поточну ітерацію і зупиняє серію. Уже виконані вимірювання зберігаються та відправляються як часткова серія, код виходу 130. Другий `Ctrl+C` завершує програму негайно.
- Якщо сервер недоступний, runner робить 3 спроби з паузою 2 секунди. Після невдачі результат зберігається у файл у папці `results/` (інша папка задається параметром `--results-dir`), а в консолі виводиться команда для повторної відправки.
- Відправити збережене пізніше: `PerfCompare.Runner send results --server http://localhost:5000`. Можна вказати окремий файл або папку. Відправлені файли перейменовуються на `*.json.sent` і повторно не відправляються.

Коди виходу runner:

| Код | Значення |
|---|---|
| 0 | серію виконано, результат збережено або відправлено |
| 1 | результат не вдалося відправити (його збережено у файл) |
| 2 | неправильна команда або некоректний сценарій |
| 130 | серію перервано користувачем |

## Релізи

Реліз створюється автоматично, коли в репозиторій надсилається тег виду `v0.2` чи `v1.0`. Для `win-x64`, `linux-x64` і `osx-arm64` збираються автономні архіви (`self-contained`, один файл на програму), які запускаються без встановленого .NET. Перед публікацією кожен архів проходить перевірку: сервер і runner запускаються з розпакованої папки, запуск потрапляє на сервер.

## Документація

- `docs/contract.md`: контракт між runner і server
- `docs/decisions.md`: журнал рішень

