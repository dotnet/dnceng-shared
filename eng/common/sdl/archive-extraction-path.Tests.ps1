$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0

. "$PSScriptRoot\archive-extraction-path.ps1"

function Assert-Equal {
  param(
    [Parameter(Mandatory=$true)] $Expected,
    [Parameter(Mandatory=$true)] $Actual,
    [Parameter(Mandatory=$true)][string] $Message
  )

  if ($Expected -ne $Actual) {
    throw "$Message Expected '$Expected', got '$Actual'."
  }
}

function Assert-Rejected {
  param(
    [Parameter(Mandatory=$true)][string] $Root,
    [Parameter(Mandatory=$true)][string] $EntryName
  )

  try {
    Get-ValidatedArchiveEntryPath -ExtractionRoot $Root -EntryName $EntryName | Out-Null
  }
  catch {
    return
  }

  throw "Expected archive entry '$EntryName' to be rejected."
}

function New-TestPackage {
  param(
    [Parameter(Mandatory=$true)][string] $Path,
    [Parameter(Mandatory=$true)][string[]] $EntryNames
  )

  Add-Type -AssemblyName System.IO.Compression.FileSystem
  $Stream = [System.IO.File]::Create($Path)
  try {
    $Archive = [System.IO.Compression.ZipArchive]::new(
      $Stream,
      [System.IO.Compression.ZipArchiveMode]::Create,
      $false)
    try {
      foreach ($EntryName in $EntryNames) {
        $Entry = $Archive.CreateEntry($EntryName)
        $EntryStream = $Entry.Open()
        try {
          $EntryStream.WriteByte(1)
        }
        finally {
          $EntryStream.Dispose()
        }
      }
    }
    finally {
      $Archive.Dispose()
    }
  }
  finally {
    $Stream.Dispose()
  }
}

$TestRoot = Join-Path ([System.IO.Path]::GetTempPath()) ([System.IO.Path]::GetRandomFileName())
[System.IO.Directory]::CreateDirectory($TestRoot) | Out-Null
try {
  $ExtractionRoot = Join-Path $TestRoot 'root'
  [System.IO.Directory]::CreateDirectory($ExtractionRoot) | Out-Null

  $InvalidEntries = @(
    '../../../evil.dll',
    '..\..\..\evil.dll',
    '../..\evil.dll',
    '/evil.dll',
    '\evil.dll',
    'C:\evil.dll',
    'C:/evil.dll',
    'C:evil.dll',
    '\\server\share\evil.dll',
    '//server/share/evil.dll',
    '..\root-evil\evil.dll',
    '../../outside/',
    '..\..\outside\'
  )
  foreach ($EntryName in $InvalidEntries) {
    Assert-Rejected -Root $ExtractionRoot -EntryName $EntryName
  }

  $ExpectedSafePath = Join-Path $ExtractionRoot 'safe.dll'
  $ActualSafePath = Get-ValidatedArchiveEntryPath -ExtractionRoot $ExtractionRoot -EntryName 'nested/../safe.dll'
  Assert-Equal $ExpectedSafePath $ActualSafePath 'A normalized path within the root should be accepted.'

  $ExpectedNestedPath = Join-Path (Join-Path $ExtractionRoot 'nested') 'safe.pdb'
  $ActualNestedPath = Get-ValidatedArchiveEntryPath -ExtractionRoot $ExtractionRoot -EntryName 'nested\safe.pdb'
  Assert-Equal $ExpectedNestedPath $ActualNestedPath 'Both archive separator styles should be normalized.'

  Get-ValidatedArchiveEntryPath -ExtractionRoot $ExtractionRoot -EntryName 'nested/' | Out-Null
  Get-ValidatedArchiveEntryPath -ExtractionRoot $ExtractionRoot -EntryName 'nested\' | Out-Null

  if ([System.IO.Path]::DirectorySeparatorChar -eq '\') {
    Get-ValidatedArchiveEntryPath -ExtractionRoot $ExtractionRoot -EntryName '..\ROOT\safe.dll' | Out-Null
  } else {
    Assert-Rejected -Root $ExtractionRoot -EntryName '..\ROOT\safe.dll'
  }

  $ValidInput = Join-Path $TestRoot 'valid-input'
  $ValidOutput = Join-Path $TestRoot 'valid-output'
  [System.IO.Directory]::CreateDirectory($ValidInput) | Out-Null
  New-TestPackage -Path (Join-Path $ValidInput 'valid.nupkg') -EntryNames @(
    'lib/nested.dll',
    'tools\nested.exe',
    'symbols/nested.pdb'
  )

  $ValidRunOutput = & (Get-Process -Id $PID).Path -NoLogo -NoProfile `
    -File "$PSScriptRoot\extract-artifact-packages.ps1" `
    -InputPath $ValidInput -ExtractPath $ValidOutput 2>&1
  if ($LASTEXITCODE -ne 0) {
    throw "Valid package extraction should succeed. Output: $($ValidRunOutput -join [Environment]::NewLine)"
  }
  foreach ($RelativePath in @('lib\nested.dll', 'tools\nested.exe', 'symbols\nested.pdb')) {
    if (!(Test-Path (Join-Path (Join-Path $ValidOutput 'valid') $RelativePath))) {
      throw "Expected valid archive entry '$RelativePath' to be extracted."
    }
  }

  $InvalidInput = Join-Path $TestRoot 'invalid-input'
  $InvalidOutput = Join-Path $TestRoot 'invalid-output'
  [System.IO.Directory]::CreateDirectory($InvalidInput) | Out-Null
  New-TestPackage -Path (Join-Path $InvalidInput 'invalid.nupkg') -EntryNames @(
    'safe.dll',
    '../../outside.dll'
  )

  $InvalidRunOutput = & (Get-Process -Id $PID).Path -NoLogo -NoProfile `
    -File "$PSScriptRoot\extract-artifact-packages.ps1" `
    -InputPath $InvalidInput -ExtractPath $InvalidOutput 2>&1
  if ($LASTEXITCODE -eq 0) {
    throw "Malicious package extraction should fail. Output: $($InvalidRunOutput -join [Environment]::NewLine)"
  }
  if (Test-Path (Join-Path $TestRoot 'outside.dll')) {
    throw 'Malicious package extraction created a file outside the extraction directory.'
  }
  if (Test-Path (Join-Path (Join-Path $InvalidOutput 'invalid') 'safe.dll')) {
    throw 'Archive entries were extracted before the malicious path was rejected.'
  }
}
finally {
  Remove-Item -Path $TestRoot -Recurse -Force
}
