$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$sourceRoot = Join-Path $root 'src\HSScanner'

$forbidden = @(
    'Path.GetTempPath',
    'Environment.SpecialFolder.ApplicationData',
    'Environment.SpecialFolder.LocalApplicationData',
    'File.Delete(',
    'Directory.Delete(',
    'File.Move(',
    'Directory.Move(',
    'File.Copy(',
    'File.SetAttributes(',
    'Directory.CreateDirectory('
)

$sourceFiles = Get-ChildItem $sourceRoot -Recurse -File | Where-Object { $_.Extension -in '.cs', '.xaml' }
foreach ($needle in $forbidden) {
    $matches = $sourceFiles | Select-String -SimpleMatch $needle
    if ($matches) {
        throw "Forbidden source-write/storage API '$needle' found: $($matches.Path -join ', ')"
    }
}

$outputService = Get-Content (Join-Path $sourceRoot 'Infrastructure\OutputPathService.cs') -Raw
if ($outputService -notmatch 'AppDomain\.CurrentDomain\.BaseDirectory') {
    throw 'OutputPathService must root all outputs at AppDomain.CurrentDomain.BaseDirectory.'
}

$writer = Get-Content (Join-Path $sourceRoot 'Services\SimpleXlsxWriter.cs') -Raw
if ($writer -notmatch 'FileMode\.CreateNew') {
    throw 'Excel export must use FileMode.CreateNew so existing files cannot be overwritten.'
}

$project = Get-Content (Join-Path $sourceRoot 'HSScanner.csproj') -Raw
if ($project -notmatch '<TargetFrameworkVersion>v4\.7\.2</TargetFrameworkVersion>') {
    throw 'HSScanner must target .NET Framework 4.7.2 for the intended Windows 10 environment.'
}
if ($project -match '<PackageReference') {
    throw 'HSScanner must not add NuGet PackageReference dependencies.'
}

Write-Host 'Static safety checks passed.'
