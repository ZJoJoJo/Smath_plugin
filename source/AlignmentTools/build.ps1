param(
    [Parameter(Mandatory = $true)][string]$SMathPath,
    [switch]$Package,
    [string]$OutputDirectory = (Join-Path $PSScriptRoot 'build'),
    [string]$PackageDirectory = (Join-Path $PSScriptRoot 'packages')
)
$ErrorActionPreference = 'Stop'
$SMathPath = (Resolve-Path -LiteralPath $SMathPath).Path
$plugin = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'plugin.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw '.NET Framework 4.x C# compiler was not found.' }
$references = foreach ($name in @('SMath.Controls.dll','SMath.Document.dll','SMath.Manager.dll','SMath.Math.Numeric.dll','SMath.Math.Symbolic.dll','SMath.Drawing.dll')) {
    $path = Join-Path $SMathPath $name
    if (-not (Test-Path -LiteralPath $path)) { throw ('Missing SMath reference: ' + $path) }
    '/reference:' + $path
}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
$sources = @(Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.cs' -File | Select-Object -ExpandProperty FullName)
$sources += @(Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'Common') -Filter '*.cs' -File | Select-Object -ExpandProperty FullName)
$mainFile = $plugin.Assembly + '.dll'
& $compiler /nologo /target:library /optimize+ /codepage:65001 ("/out:" + (Join-Path $OutputDirectory $mainFile)) /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Core.dll /reference:System.Numerics.dll /reference:System.Xml.dll /reference:System.Web.Extensions.dll /reference:Microsoft.CSharp.dll @references @sources
if ($LASTEXITCODE -ne 0) { throw ('Compilation failed: ' + $plugin.Assembly) }
if ($plugin.Assembly -eq 'MathNotes') {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'assets') -Destination $OutputDirectory -Recurse -Force
    foreach ($entry in Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'dependencies\WebView2')) {
        Copy-Item -LiteralPath $entry.FullName -Destination $OutputDirectory -Recurse -Force
    }
}
$metadata = [ordered]@{ Author='Local engineering tools';Description=$plugin.Description;Guid=$plugin.Id;Title=$plugin.Title;Version=$plugin.Version } | ConvertTo-Json -Compress
[IO.File]::WriteAllText((Join-Path $OutputDirectory ($mainFile + '.ini')), $metadata, [Text.UTF8Encoding]::new($false))
Write-Output ('Built: ' + (Join-Path $OutputDirectory $mainFile))
if ($Package) {
    foreach ($file in Get-ChildItem -LiteralPath $SMathPath -Filter 'SMath.*.dll') { [void][Reflection.Assembly]::LoadFrom($file.FullName) }
    [void][IO.Directory]::CreateDirectory($PackageDirectory)
    $packageName = [SMath.Manager.PluginInstallerUtility]::AppendVersionToFileName(($plugin.Assembly + '.zip'), [Version]$plugin.Version)
    $archivePath = Join-Path $PackageDirectory $packageName
    $archive = [SMath.Manager.Archiver]::Create($archivePath, '')
    try {
        $archive.AddFile((Join-Path $OutputDirectory $mainFile), $mainFile, $plugin.Id)
        $outputRoot = [IO.Path]::GetFullPath($OutputDirectory).TrimEnd('\','/')
        foreach ($file in Get-ChildItem -LiteralPath $outputRoot -Recurse -File | Where-Object { $_.FullName -ne (Join-Path $outputRoot $mainFile) }) {
            $relative = $file.FullName.Substring($outputRoot.Length + 1).Replace('\','/')
            $archive.AddFile($file.FullName, $relative, '')
        }
    } finally { $archive.Dispose() }
    Write-Output ('Packaged: ' + $archivePath)
}
