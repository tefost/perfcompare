# Сценарій демонстрації

Демонстрація показує повний цикл на двох операційних системах одночасно: сервер працює в Linux (WSL), runner запускається і в Windows, і в Linux, а дашборд порівнює обидва середовища. Усі кроки виконуються з готових релізних архівів, тобто без встановленого .NET.

## Підготовка (один раз)

Завантажте архіви останнього релізу (PowerShell):

```powershell
gh release download v1.0.0 --repo tefost/perfcompare --pattern "perfcompare-*" --dir "$env:USERPROFILE\perfdemo"
Expand-Archive "$env:USERPROFILE\perfdemo\perfcompare-win-x64.zip" -DestinationPath "$env:USERPROFILE\perfdemo\win"
```

Розпакуйте архів Linux у WSL:

```powershell
wsl mkdir -p ~/perfdemo
wsl tar -xzf /mnt/c/Users/$env:USERNAME/perfdemo/perfcompare-linux-x64.tar.gz -C ~/perfdemo
```

## Крок 1. Сервер у Linux

Перше вікно PowerShell:

```powershell
wsl bash -c "cd ~/perfdemo && chmod +x PerfCompare.Server PerfCompare.Runner && ./PerfCompare.Server --urls http://0.0.0.0:5000"
```

Очікується: рядок `Now listening on: http://0.0.0.0:5000`. Дашборд у браузері Windows: `http://localhost:5000/`.

## Крок 2. Runner у Windows

Друге вікно PowerShell:

```powershell
cd "$env:USERPROFILE\perfdemo\win"
.\PerfCompare.Runner.exe run scenarios\baseline.yaml --server http://localhost:5000
```

Очікується: `[прогрів 1/1]`, п'ять рядків `[n/5] Passed ...` і `Відправлено на сервер, runId=1`.

## Крок 3. Runner у Linux

Третє вікно PowerShell:

```powershell
wsl bash -c "cd ~/perfdemo && ./PerfCompare.Runner run scenarios/baseline.yaml --server http://localhost:5000"
```

Очікується: `Відправлено на сервер, runId=2`. На дашборді (оновіть сторінку, виберіть сценарій `baseline`) у таблиці порівняння два середовища: Windows і Linux, з медіаною, P95, стабільністю й тим, у скільки разів одне повільніше за інше.

## Крок 4. Регресія

Позначте запуск Windows базовим кнопкою на дашборді. Виконайте важчий сценарій із тією самою назвою:

```powershell
cd "$env:USERPROFILE\perfdemo\win"
.\PerfCompare.Runner.exe run scenarios\slow.yaml --server http://localhost:5000
```

Оновіть дашборд: у рядку Windows з'явиться позначка регресії з відсотком погіршення. Натисніть «Експорт CSV» і «HTML-звіт» і покажіть, що звіт відкривається окремою сторінкою.

## Крок 5. Крайні випадки

Зупиніть сервер (`Ctrl+C` у першому вікні) і виконайте:

```powershell
cd "$env:USERPROFILE\perfdemo\win"
.\PerfCompare.Runner.exe run scenarios\baseline.yaml --server http://localhost:5000
```

Очікується: три спроби, повідомлення про збереження файлу в `results` і команда повторної відправки, код виходу 1 (`$LASTEXITCODE`). Запустіть сервер знову (крок 1) і відправте збережене:

```powershell
.\PerfCompare.Runner.exe send results --server http://localhost:5000
```

Очікується: `Відправлено: 1, не вдалося: 0`, файл у `results` перейменовано на `.json.sent`. Для порожнього або зламаного сценарію:

```powershell
.\PerfCompare.Runner.exe run no-such.yaml
```

Очікується: повідомлення «Помилка сценарію» без стеку виклику, код 2.

## Що показати з репозиторію

| Тема | Де |
|---|---|
| тести, покриття, CI на трьох ОС | вкладка Actions, `docs/reports/coverage-server.md`, `docs/reports/coverage-runner.md`, `docs/reports/test-results.md` |
| статичний аналіз | `docs/reports/analyzer-report.md` |
| тест-кейси | `docs/test-cases.md` |
| крайні випадки | `docs/edge-cases-server.md`, `docs/edge-cases-runner.md` |
| архітектура і схема бази | `docs/architecture.md` |
| історія роботи | `git log`, Pull Requests, журнал рішень `docs/decisions.md`, гілка main з правилами захисту |
| запуск з нуля | розділ «Перевірка з нуля» в README |
| Linux і macOS без WSL | job `e2e` і smoke test у `release.yml` на ubuntu і macos |
