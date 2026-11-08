<#
.SYNOPSIS
Запускає тести проєкту з вимірюванням покриття і будує звіт у docs/reports.

.EXAMPLE
./scripts/coverage.ps1 -Name Server
#>
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("Server", "Runner")]
    [string]$Name,

    [double]$Threshold = 80
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$lower = $Name.ToLowerInvariant()
$project = "tests/PerfCompare.$Name.Tests"
$raw = Join-Path $root "coverage/$lower"
$html = Join-Path $root "coverage/$lower-report"
$reports = Join-Path $root "docs/reports"
$target = Join-Path $reports "coverage-$lower.md"

foreach ($folder in @($raw, $html)) {
    if (Test-Path $folder) {
        Remove-Item $folder -Recurse -Force
    }
}

New-Item -ItemType Directory -Force -Path $reports | Out-Null

dotnet test $project --collect:"XPlat Code Coverage" --results-directory $raw
if ($LASTEXITCODE -ne 0) {
    throw "Тести завершились з помилкою, звіт покриття не побудовано."
}

$cobertura = Get-ChildItem $raw -Recurse -Filter "coverage.cobertura.xml" | Select-Object -First 1
if ($null -eq $cobertura) {
    throw "Файл coverage.cobertura.xml не знайдено. Перевірте, що тестовий проєкт містить пакет coverlet.collector."
}

dotnet tool restore
if ($LASTEXITCODE -ne 0) {
    throw "Не вдалося відновити інструменти з .config/dotnet-tools.json."
}

$coverageFile = $cobertura.FullName
dotnet tool run reportgenerator "-reports:$coverageFile" "-targetdir:$html" "-reporttypes:TextSummary;MarkdownSummaryGithub" "-assemblyfilters:+PerfCompare.$Name"
if ($LASTEXITCODE -ne 0) {
    throw "ReportGenerator завершився з помилкою."
}

$summary = Get-Content (Join-Path $html "Summary.txt") -Raw
$markdown = Get-Content (Join-Path $html "SummaryGithub.md") -Raw

function Read-Percent([string]$label) {
    $match = [regex]::Match($summary, "$label coverage:\s*([\d]+[.,]?[\d]*)%")
    if (-not $match.Success) {
        throw "У звіті немає рядка '$label coverage'."
    }

    return [double]::Parse($match.Groups[1].Value.Replace(",", "."), [System.Globalization.CultureInfo]::InvariantCulture)
}

$lines = Read-Percent "Line"
$branches = Read-Percent "Branch"
$ok = $lines -ge $Threshold
$verdict = if ($ok) { "поріг виконано" } else { "нижче порога" }
$today = (Get-Date).ToString("yyyy-MM-dd")

$text = @(
    "# Покриття тестами: $Name",
    "",
    "Дата: $today. Засоби: coverlet.collector (вимірювання), ReportGenerator (звіт). Збірка тестів: $project.",
    "",
    "Команда для відтворення: ``./scripts/coverage.ps1 -Name $Name``.",
    "",
    "| Показник | Значення |",
    "|---|---|",
    "| Покриття рядків | $lines% |",
    "| Покриття гілок | $branches% |",
    "| Поріг для рядків | $Threshold% |",
    "| Висновок | $verdict |",
    "",
    "## Звіт ReportGenerator",
    "",
    $markdown
) -join [Environment]::NewLine

Set-Content -Path $target -Value $text -Encoding utf8
Write-Host "Покриття рядків ${lines}%, гілок ${branches}%. Звіт: docs/reports/coverage-$lower.md"

if (-not $ok) {
    throw "Покриття рядків $lines% нижче порога $Threshold%."
}
