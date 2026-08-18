$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$sourceRoot = Join-Path $root 'src\HSTemplate'
$projectPath = Join-Path $sourceRoot 'HSTemplate.csproj'

if (-not (Test-Path $projectPath)) {
    throw 'HSTemplate project is missing.'
}

$project = Get-Content $projectPath -Raw
if ($project -notmatch '<TargetFrameworkVersion>v4\.7\.2</TargetFrameworkVersion>') {
    throw 'Template must target .NET Framework 4.7.2.'
}
if ($project -match '<PackageReference') {
    throw 'Template must not contain NuGet PackageReference dependencies.'
}

$sourceFiles = Get-ChildItem $sourceRoot -Recurse -File | Where-Object { $_.Extension -in '.cs', '.xaml' }
$forbiddenStorage = @(
    'Path.GetTempPath',
    'Environment.SpecialFolder.ApplicationData',
    'Environment.SpecialFolder.LocalApplicationData'
)
foreach ($needle in $forbiddenStorage) {
    $matches = $sourceFiles | Select-String -SimpleMatch $needle
    if ($matches) {
        throw "Template source uses forbidden default storage API '$needle'."
    }
}

$outputService = Get-Content (Join-Path $sourceRoot 'Infrastructure\OutputPathService.cs') -Raw
if ($outputService -notmatch 'AppDomain\.CurrentDomain\.BaseDirectory') {
    throw 'OutputPathService must root generated output beside the executable.'
}
if ($outputService -notmatch 'FileMode\.CreateNew') {
    throw 'OutputPathService must expose non-overwriting CreateNew output.'
}

foreach ($required in @('Themes\Colors.xaml', 'Themes\Controls.xaml', 'MainWindow.xaml', 'Infrastructure\SuiteLauncherService.cs')) {
    if (-not (Test-Path (Join-Path $sourceRoot $required))) {
        throw "Required suite UI/infrastructure file missing: $required"
    }
}

$controls = Get-Content (Join-Path $sourceRoot 'Themes\Controls.xaml') -Raw
if ($controls -notmatch '<Trigger Property="TextAlignment" Value="Center">\s*<Setter Property="Padding" Value="4,0" />\s*</Trigger>') {
    throw 'HsTextBoxStyle must reduce horizontal padding for center-aligned compact inputs.'
}
if ($controls -notmatch 'x:Key="HsLogoButtonStyle"') {
    throw 'Template must define the canonical HS logo/Home button style.'
}

$homeService = Get-Content (Join-Path $sourceRoot 'Infrastructure\SuiteLauncherService.cs') -Raw
foreach ($requiredFragment in @('AppDomain.CurrentDomain.BaseDirectory', '"HSSuite.exe"', '"HSSuite-v*.exe"', 'SearchOption.TopDirectoryOnly')) {
    if (-not $homeService.Contains($requiredFragment)) {
        throw "Template Home service is missing required behavior: $requiredFragment"
    }
}

$launcherRoot = Join-Path $root 'src\HSSuite'
$launcherProject = Join-Path $launcherRoot 'HSSuite.csproj'
if (Test-Path $launcherProject) {
    $launcherProjectText = Get-Content $launcherProject -Raw
    if ($launcherProjectText -notmatch '<TargetFrameworkVersion>v4\.7\.2</TargetFrameworkVersion>') {
        throw 'HSSuite launcher must target .NET Framework 4.7.2.'
    }
    if ($launcherProjectText -match '<PackageReference') {
        throw 'HSSuite launcher must not contain NuGet PackageReference dependencies.'
    }

    $discovery = Get-Content (Join-Path $launcherRoot 'Services\AppDiscoveryService.cs') -Raw
    foreach ($requiredFragment in @('"HS*.exe"', 'SearchOption.TopDirectoryOnly', '"HSSuite.exe"')) {
        if (-not $discovery.Contains($requiredFragment)) {
            throw "HSSuite discovery is missing required local-only behavior: $requiredFragment"
        }
    }

    $templateColors = Get-Content (Join-Path $sourceRoot 'Themes\Colors.xaml') -Raw
    $launcherColors = Get-Content (Join-Path $launcherRoot 'Themes\Colors.xaml') -Raw
    if ($templateColors -ne $launcherColors) {
        throw 'HSSuite launcher Colors.xaml must match the canonical template palette.'
    }

    $templateControls = Get-Content (Join-Path $sourceRoot 'Themes\Controls.xaml') -Raw
    $launcherControls = Get-Content (Join-Path $launcherRoot 'Themes\Controls.xaml') -Raw
    if ($templateControls -ne $launcherControls) {
        throw 'HSSuite launcher Controls.xaml must match the canonical template controls.'
    }
}

$initializer = Get-Content (Join-Path $root 'scripts\Initialize-App.ps1') -Raw
foreach ($requiredFragment in @("'src\HSSuite'", "'HSSuite.sln'", 'Remove-Item -LiteralPath $launcherProject')) {
    if (-not $initializer.Contains($requiredFragment)) {
        throw "Initializer must remove repository-only launcher content: $requiredFragment"
    }
}

$releaseWorkflow = Get-Content (Join-Path $root '.github\workflows\release.yml') -Raw
$releaseRequirements = @(
    'git push origin "refs/tags/${tag}:refs/tags/${tag}"',
    '--verify-tag',
    'git ls-remote --tags origin "refs/tags/$tag"',
    'git push origin ":refs/heads/$branch"',
    'Release tag disappeared during branch cleanup',
    "'src/HSSuite/HSSuite.csproj'"
)
foreach ($requiredFragment in $releaseRequirements) {
    if (-not $releaseWorkflow.Contains($requiredFragment)) {
        throw "Release workflow is missing required tag/launcher invariant: $requiredFragment"
    }
}

if ($releaseWorkflow.Contains('refs/tags/$tag:refs/tags/$tag')) {
    throw 'PowerShell tag refspec must use ${tag} before a colon to avoid scoped-variable parsing.'
}

Write-Host 'HSSuite template and launcher invariants passed.'
