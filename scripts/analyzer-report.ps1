<#
.SYNOPSIS
Збирає рішення з аналізаторами SonarAnalyzer і будує звіт про попередження в docs/reports/analyzer-report.md.

.EXAMPLE
./scripts/analyzer-report.ps1
#>
param(
    [int]$MaxWarnings = -1
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$reports = Join-Path $root "docs/reports"
New-Item -ItemType Directory -Force -Path $reports | Out-Null

$output = dotnet build PerfCompare.sln --no-incremental --configuration Release -v:minimal 2>&1 | Out-String
if ($LASTEXITCODE -ne 0) {
    Write-Host $output
    throw "Збірка завершилась з помилкою, звіт не побудовано."
}

$pattern = "(?m)^(?<file>.+?)\((?<line>\d+),\d+\): warning (?<rule>[A-Za-z]+\d+): (?<message>.+?) \["
$found = [regex]::Matches($output, $pattern) | ForEach-Object {
    [pscustomobject]@{
        File    = $_.Groups["file"].Value.Replace($root, "").TrimStart("\", "/").Replace("\", "/")
        Line    = [int]$_.Groups["line"].Value
        Rule    = $_.Groups["rule"].Value
        Message = $_.Groups["message"].Value
    }
}

$unique = @($found | Sort-Object File, Line, Rule -Unique)

$sources = Get-ChildItem (Join-Path $root "src") -Recurse -Filter "*.cs" |
    Where-Object { $_.FullName -notmatch "[\\/](obj|bin)[\\/]" }
$codeLines = 0
foreach ($source in $sources) {
    $codeLines += @(Get-Content $source.FullName | Where-Object { $_.Trim().Length -gt 0 }).Count
}

$sonar = @($unique | Where-Object { $_.Rule -like "S*" })
$other = @($unique | Where-Object { $_.Rule -notlike "S*" })
$per1000 = if ($codeLines -gt 0) { [math]::Round($sonar.Count * 1000.0 / $codeLines, 2) } else { 0 }
$today = (Get-Date).ToString("yyyy-MM-dd")

$text = New-Object System.Collections.Generic.List[string]
$text.Add("# Звіт статичного аналізу")
$text.Add("")
$text.Add("Дата: $today. Засіб: SonarAnalyzer.CSharp (версія з Directory.Build.props), запускається під час збірки (dotnet build --no-incremental --configuration Release).")
$text.Add("")
$text.Add("Команда для відтворення: ``./scripts/analyzer-report.ps1``.")
$text.Add("")
$text.Add("| Показник | Значення |")
$text.Add("|---|---|")
$text.Add("| Рядків коду в src (без порожніх) | $codeLines |")
$text.Add("| Попередження SonarAnalyzer (правила S*) | $($sonar.Count) |")
$text.Add("| Інші попередження компілятора й аналізаторів | $($other.Count) |")
$text.Add("| Попереджень SonarAnalyzer на 1000 рядків | $per1000 |")
$text.Add("")
$text.Add("У звіт потрапляють усі попередження незалежно від рівня серйозності, тому кількість Critical і Major не більша за наведену.")
$text.Add("")

if ($unique.Count -eq 0) {
    $text.Add("Попереджень немає: збірка пройшла без жодного попередження аналізаторів.")
}
else {
    $text.Add("## Попередження за правилами")
    $text.Add("")
    $text.Add("| Правило | Кількість |")
    $text.Add("|---|---|")
    foreach ($group in ($unique | Group-Object Rule | Sort-Object Count -Descending)) {
        $text.Add("| $($group.Name) | $($group.Count) |")
    }

    $text.Add("")
    $text.Add("## Перелік")
    $text.Add("")
    $text.Add("| Файл | Рядок | Правило | Повідомлення |")
    $text.Add("|---|---|---|---|")
    foreach ($item in $unique) {
        $message = $item.Message.Replace("|", "\|")
        $text.Add("| $($item.File) | $($item.Line) | $($item.Rule) | $message |")
    }
}

$target = Join-Path $reports "analyzer-report.md"
Set-Content -Path $target -Value ($text -join [Environment]::NewLine) -Encoding utf8
Write-Host "Попереджень SonarAnalyzer: $($sonar.Count), інших: $($other.Count), рядків коду: $codeLines. Звіт: docs/reports/analyzer-report.md"

if ($MaxWarnings -ge 0 -and $unique.Count -gt $MaxWarnings) {
    throw "Попереджень $($unique.Count), дозволено не більше $MaxWarnings."
}
