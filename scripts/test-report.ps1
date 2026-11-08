<#
.SYNOPSIS
Запускає всі тести рішення і записує результат за класами в docs/reports/test-results.md.

.EXAMPLE
./scripts/test-report.ps1
#>
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$raw = Join-Path $root "coverage/trx"
if (Test-Path $raw) {
    Remove-Item $raw -Recurse -Force
}

$reports = Join-Path $root "docs/reports"
New-Item -ItemType Directory -Force -Path $reports | Out-Null

dotnet test PerfCompare.sln --logger "trx" --results-directory $raw
$testExit = $LASTEXITCODE

$rows = New-Object System.Collections.Generic.List[object]
foreach ($file in Get-ChildItem $raw -Filter "*.trx") {
    [xml]$trx = Get-Content $file.FullName -Raw -Encoding utf8
    $ns = New-Object System.Xml.XmlNamespaceManager($trx.NameTable)
    $ns.AddNamespace("t", "http://microsoft.com/schemas/VisualStudio/TeamTest/2010")

    $classes = @{}
    foreach ($definition in $trx.SelectNodes("//t:UnitTest", $ns)) {
        $method = $definition.SelectSingleNode("t:TestMethod", $ns)
        $classes[$definition.Attributes["id"].Value] = $method.Attributes["className"].Value
    }

    foreach ($result in $trx.SelectNodes("//t:UnitTestResult", $ns)) {
        $full = $classes[$result.Attributes["testId"].Value]
        $assembly = ($full -split "\.")[0..1] -join "."
        $rows.Add([pscustomobject]@{
            Project = $assembly
            Class   = ($full -split "\.")[-1]
            Outcome = $result.Attributes["outcome"].Value
            Name    = $result.Attributes["testName"].Value
        })
    }
}

if ($rows.Count -eq 0) {
    throw "У файлах trx немає результатів тестів."
}

$total = $rows.Count
$passed = @($rows | Where-Object { $_.Outcome -eq "Passed" }).Count
$failedRows = @($rows | Where-Object { $_.Outcome -ne "Passed" })
$today = (Get-Date).ToString("yyyy-MM-dd")

$text = New-Object System.Collections.Generic.List[string]
$text.Add("# Результат запуску тестів")
$text.Add("")
$text.Add("Дата: $today. Команда: ``dotnet test PerfCompare.sln``, звіт створено скриптом ``./scripts/test-report.ps1``.")
$text.Add("")
$text.Add("| Показник | Значення |")
$text.Add("|---|---|")
$text.Add("| Усього тестів | $total |")
$text.Add("| Пройшло | $passed |")
$text.Add("| Не пройшло чи пропущено | $($failedRows.Count) |")
$text.Add("")
$text.Add("## За класами")
$text.Add("")
$text.Add("| Проєкт | Клас | Тестів | Пройшло |")
$text.Add("|---|---|---|---|")
foreach ($group in ($rows | Group-Object Project, Class | Sort-Object Name)) {
    $first = $group.Group[0]
    $ok = @($group.Group | Where-Object { $_.Outcome -eq "Passed" }).Count
    $text.Add("| $($first.Project) | $($first.Class) | $($group.Count) | $ok |")
}

if ($failedRows.Count -gt 0) {
    $text.Add("")
    $text.Add("## Тести, що не пройшли")
    $text.Add("")
    foreach ($item in $failedRows) {
        $text.Add("- $($item.Class).$($item.Name): $($item.Outcome)")
    }
}

Set-Content -Path (Join-Path $reports "test-results.md") -Value ($text -join [Environment]::NewLine) -Encoding utf8
Write-Host "Тестів: $total, пройшло: $passed. Звіт: docs/reports/test-results.md"

if ($testExit -ne 0) {
    throw "Деякі тести не пройшли, див. звіт."
}
