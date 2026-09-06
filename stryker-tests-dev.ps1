#!/usr/bin/env pwsh
#Requires -Version 7

<#
.SYNOPSIS
    Runs Stryker.NET mutation testing over the Lumoin.Vericula core and MessageFormat projects.

.DESCRIPTION
    Stryker mutates one project per run; the scope (stryker-config.json) is
    src/Lumoin.Vericula's sources, excluding the WellKnown* vocabularies and
    Text/Utf8Constants.cs, with Lumoin.Vericula.Tests as the killer population.
    Stryker resolves the solution from the config's "solution" entry
    (Lumoin.Vericula.slnx); per-project HTML reports land under StrykerOutput/
    at the repo root.

    KNOWN BLOCKER - Stryker cannot run this suite yet (VERIFIED against 4.16.0, 2026-09-02):
    the test projects run on Microsoft.Testing.Platform (the MSTest.Sdk default runner), which
    Stryker.NET discovers through VsTest - it finds 0 tests ("Test assemblies do not contain
    any test, skipping") and every mutant reports not-fully-tested with no mutation score.
    Track upstream:
      https://github.com/stryker-mutator/stryker-net/issues/3094
    Until that resolves (or the test project additionally exposes a VsTest-compatible
    adapter), this invocation is correct and ready for the day the blocker lifts.
#>

$ErrorActionPreference = 'Stop'

$projects = @(
    'Lumoin.Vericula'
    'Lumoin.Vericula.MessageFormat'
    # 'Lumoin.Vericula.SourceGenerators'
    # - add its own entry once test/Lumoin.Vericula.SourceGenerators.Tests is wired
    #   into "test-projects" in stryker-config.json alongside Lumoin.Vericula.Tests.
)

$configFiles = @{
    'Lumoin.Vericula'              = 'stryker-config.json'
    'Lumoin.Vericula.MessageFormat' = 'stryker-config.messageformat.json'
}

$failed = @()
foreach($project in $projects)
{
    Write-Host "=== Stryker: mutating $project ===" -ForegroundColor Cyan
    dotnet dotnet-stryker `
        --config-file $configFiles[$project] `
        --reporter progress `
        --reporter html `
        --project "$project.csproj" `
        --output "StrykerOutput/$project"

    if($LASTEXITCODE -ne 0)
    {
        Write-Warning "Stryker exited with code $LASTEXITCODE for $project. Continuing with remaining projects."
        $failed += $project
    }
}

if($failed.Count -gt 0)
{
    Write-Host "Stryker failed for: $($failed -join ', ')." -ForegroundColor Yellow
    exit 1
}
