[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [string]$Runtime = 'win-x64',
    [string]$IsccPath = '',
    [switch]$SkipInstaller
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$artifactsRoot = Join-Path $repositoryRoot 'artifacts\release'
$solutionPath = Join-Path $repositoryRoot 'RePKG-Workbench.sln'
$projectPath = Join-Path $repositoryRoot 'src\RePKG.App\RePKG.App.csproj'
$installerScript = Join-Path $PSScriptRoot 'RePKG-Workbench.iss'
$frameworkOutput = Join-Path $artifactsRoot "publish\framework-dependent\$Runtime"
$portableOutput = Join-Path $artifactsRoot "publish\portable\$Runtime"
$packageOutput = Join-Path $artifactsRoot 'packages'

function Assert-LastExitCode([string]$Operation) {
    if ($LASTEXITCODE -ne 0) {
        throw "$Operation failed with exit code $LASTEXITCODE."
    }
}

function Reset-BuildDirectory([string]$Path) {
    $fullPath = [IO.Path]::GetFullPath($Path)
    $allowedPrefix = [IO.Path]::GetFullPath($artifactsRoot).TrimEnd('\') + '\'
    if (-not $fullPath.StartsWith($allowedPrefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to reset a directory outside the release artifacts root: $fullPath"
    }

    if (Test-Path -LiteralPath $fullPath) {
        Remove-Item -LiteralPath $fullPath -Recurse -Force
    }
    New-Item -ItemType Directory -Path $fullPath | Out-Null
}

function Assert-ReleaseContents([string]$PublishDirectory) {
    $requiredFiles = @(
        'RePKG-Workbench.exe',
        'RePKG-Workbench.dll',
        'RePKG-Workbench.deps.json',
        'RePKG-Workbench.runtimeconfig.json',
        'RePKG.Core.dll',
        'CommunityToolkit.Mvvm.dll',
        'WpfAnimatedGif.dll',
        'Languages\en-US.json',
        'Languages\zh-CN.json',
        'LICENSE',
        'tools\RePKG.exe',
        'tools\LICENSE-RePKG',
        'tools\SHA256SUMS'
    )

    foreach ($relativePath in $requiredFiles) {
        $candidate = Join-Path $PublishDirectory $relativePath
        if (-not (Test-Path -LiteralPath $candidate -PathType Leaf)) {
            throw "Required release file is missing: $candidate"
        }
    }

    $expectedHash = ((Get-Content -LiteralPath (Join-Path $PublishDirectory 'tools\SHA256SUMS') -Raw).Trim() -split '\s+')[0]
    $actualHash = (Get-FileHash -LiteralPath (Join-Path $PublishDirectory 'tools\RePKG.exe') -Algorithm SHA256).Hash
    if (-not [string]::Equals($expectedHash, $actualHash, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Bundled RePKG.exe SHA-256 does not match tools\SHA256SUMS."
    }
}

function Resolve-IsccPath {
    if (-not [string]::IsNullOrWhiteSpace($IsccPath)) {
        return [IO.Path]::GetFullPath($IsccPath)
    }

    $command = Get-Command 'ISCC.exe' -ErrorAction SilentlyContinue
    if ($null -ne $command) {
        return $command.Source
    }

    $registryPaths = @(
        'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\Inno Setup 6_is1',
        'HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\Inno Setup 6_is1',
        'HKLM:\Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Inno Setup 6_is1'
    )
    $registryCandidates = foreach ($registryPath in $registryPaths) {
        $registryItem = Get-ItemProperty -LiteralPath $registryPath -ErrorAction SilentlyContinue
        if ($null -eq $registryItem) {
            continue
        }
        $installLocationProperty = $registryItem.PSObject.Properties['InstallLocation']
        if ($null -ne $installLocationProperty -and -not [string]::IsNullOrWhiteSpace($installLocationProperty.Value)) {
            Join-Path ([string]$installLocationProperty.Value) 'ISCC.exe'
        }
    }
    $candidates = @($registryCandidates)
    foreach ($programFilesRoot in @(${env:ProgramFiles(x86)}, $env:ProgramFiles)) {
        if (-not [string]::IsNullOrWhiteSpace($programFilesRoot)) {
            $candidates += Join-Path $programFilesRoot 'Inno Setup 6\ISCC.exe'
        }
    }
    return $candidates | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1
}

Push-Location $repositoryRoot
try {
    & dotnet restore $solutionPath --locked-mode
    Assert-LastExitCode 'Locked restore'

    & dotnet test $solutionPath --configuration $Configuration --no-restore --disable-build-servers
    Assert-LastExitCode 'Tests'

    $versionLines = & dotnet msbuild $projectPath -nologo -getProperty:Version
    Assert-LastExitCode 'Reading the MSBuild version'
    $version = $versionLines | Where-Object { $_ -match '^\d+\.\d+\.\d+(?:[-+].*)?$' } | Select-Object -Last 1
    if ([string]::IsNullOrWhiteSpace($version)) {
        throw 'MSBuild did not return a valid Version property.'
    }

    Reset-BuildDirectory $frameworkOutput
    Reset-BuildDirectory $portableOutput
    Reset-BuildDirectory $packageOutput

    & dotnet publish $projectPath --configuration $Configuration --runtime $Runtime --self-contained false --no-restore --output $frameworkOutput -p:DebugType=None -p:DebugSymbols=false
    Assert-LastExitCode 'Framework-dependent publish'
    Assert-ReleaseContents $frameworkOutput

    & dotnet publish $projectPath --configuration $Configuration --runtime $Runtime --self-contained true --no-restore --output $portableOutput -p:DebugType=None -p:DebugSymbols=false
    Assert-LastExitCode 'Self-contained publish'
    Assert-ReleaseContents $portableOutput

    $portableArchive = Join-Path $packageOutput 'RePKG-Workbench-Portable-x64.zip'
    $frameworkPortableArchive = Join-Path $packageOutput 'RePKG-Workbench-Portable-FDD-x64.zip'
    Compress-Archive -Path (Join-Path $portableOutput '*') -DestinationPath $portableArchive -CompressionLevel Optimal
    Compress-Archive -Path (Join-Path $frameworkOutput '*') -DestinationPath $frameworkPortableArchive -CompressionLevel Optimal

    foreach ($archive in @($portableArchive, $frameworkPortableArchive)) {
        if (-not (Test-Path -LiteralPath $archive -PathType Leaf)) {
            throw "Expected portable archive was not generated: $archive"
        }
    }

    if (-not $SkipInstaller) {
        $resolvedIsccPath = Resolve-IsccPath
        if ([string]::IsNullOrWhiteSpace($resolvedIsccPath) -or -not (Test-Path -LiteralPath $resolvedIsccPath -PathType Leaf)) {
            throw 'Inno Setup 6 compiler was not found. Pass -IsccPath or use -SkipInstaller.'
        }

        & $resolvedIsccPath "/DAppVersion=$version" "/DSourceDir=$frameworkOutput" "/DOutputDir=$packageOutput" $installerScript
        Assert-LastExitCode 'Installer compilation'

        $installerPath = Join-Path $packageOutput 'RePKG-Workbench-Setup-x64.exe'
        if (-not (Test-Path -LiteralPath $installerPath -PathType Leaf)) {
            throw "Expected installer was not generated: $installerPath"
        }
    }

    $checksumLines = Get-ChildItem -LiteralPath $packageOutput -File |
        Where-Object { $_.Extension -in '.exe', '.zip' } |
        Sort-Object Name |
        ForEach-Object { "{0}  {1}" -f (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash, $_.Name }
    [IO.File]::WriteAllLines(
        (Join-Path $packageOutput 'SHA256SUMS'),
        [string[]]$checksumLines,
        [Text.UTF8Encoding]::new($false))

    $unsignedFiles = @(
        (Join-Path $frameworkOutput 'RePKG-Workbench.exe')
    )
    if (-not $SkipInstaller) {
        $unsignedFiles += Join-Path $packageOutput 'RePKG-Workbench-Setup-x64.exe'
    }
    if ($unsignedFiles | Where-Object { (Get-AuthenticodeSignature -LiteralPath $_).Status -eq 'NotSigned' }) {
        Write-Warning 'Release executables are unsigned. Sign them before public distribution to reduce SmartScreen warnings.'
    }

    Write-Host "Release $version is ready in $packageOutput"
}
finally {
    Pop-Location
}
