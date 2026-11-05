# PerfCompare

Платформа порівняння продуктивності та стабільності програмного застосунку на різних конфігураціях середовища. Runner запускає застосунок за сценарієм, збирає час, пам'ять і CPU та надсилає результати на сервер; сервер зберігає їх у SQLite, порівнює середовища і показує дашборд.

Курс: Мультиплатформне програмування, КНУ, кафедра ПСТ.

## Хто за що відповідав

| Учасник | Зона | Що зроблено | Основні PR і issue |
|---|---|---|---|
| Артем | Runner, samples, CI, реліз | _заповнити в останній день_ | _посилання_ |
| Саша | Server, БД, аналітика, дашборд | _заповнити в останній день_ | _посилання_ |

## Вимоги

- .NET 9 SDK
- Windows, Linux або macOS

## Швидкий старт

```bash
git clone <URL репозиторію>
cd perfcompare
dotnet build
dotnet test
```

## Запуск

Сервер (дашборд на `http://localhost:5000`):

```bash
dotnet run --project src/PerfCompare.Server --urls http://localhost:5000
```

Runner:

```bash
dotnet run --project src/PerfCompare.Runner -- run scenarios/baseline.yaml --server http://localhost:5000
```

## Готові збірки

Архіви для win-x64, linux-x64 і osx-arm64 лежать у розділі Releases. _Кроки запуску по ОС додати після першого релізу._

## Документація

- `docs/contract.md`: контракт між runner і server
- `docs/architecture.md`: архітектура
- `docs/decisions.md`: журнал рішень
