param(
  [string]$ProjectPath = "src/OverlayApp/OverlayApp.csproj",
  [string]$SdkPath      = "src/OverlayApp.SDK",
  [string]$PublishDir   = "publish/OverlayApp",
  [string]$DistDir      = "dist/app",
  [string]$PubXml       = ""   # e.g. "src/OverlayApp/Properties/PublishProfiles/FolderProfile.pubxml"
)

$ErrorActionPreference = "Stop"

function Assert-Tool($name) {
  if (-not (Get-Command $name -ErrorAction SilentlyContinue)) {
    throw "Required tool not found: $name"
  }
}

function Repo-Root {
  $p = Resolve-Path .
  while ($p -and -not (Test-Path (Join-Path $p ".git"))) {
    $p = Split-Path $p
  }
  if (-not $p) { throw "Not inside a Git repo." }
  Set-Location $p
}

function Get-CurrentVersion([string]$csproj) {
  if (-not (Test-Path $csproj)) { throw "csproj not found: $csproj" }
  [xml]$xml = Get-Content $csproj
  $v = $xml.Project.PropertyGroup.Version
  if (-not $v) { throw "No <Version> in $csproj" }
  return $v.Trim()
}

function Set-Version([string]$csproj, [string]$newVersion) {
  [xml]$xml = Get-Content $csproj
  $xml.Project.PropertyGroup.Version = $newVersion
  $xml.Save((Resolve-Path $csproj))
}

function SemVer-Bump([string]$v, [string]$kind) {
  if ($v -notmatch '^(?<maj>\d+)\.(?<min>\d+)\.(?<pat>\d+)(-.+)?$') { throw "Invalid SemVer: $v" }
  $maj = [int]$Matches.maj; $min = [int]$Matches.min; $pat = [int]$Matches.pat
  switch ($kind) {
    "major" { ($maj+1).ToString() + ".0.0" }
    "minor" { "$maj." + ($min+1) + ".0" }
    "patch" { "$maj.$min." + ($pat+1) }
    default { $v }
  }
}

function Last-Tag {
  $t = git tag --list "v*" --sort=-v:refname | Select-Object -First 1
  return $t
}

function Has-Changes-Since([string]$tag, [string[]]$paths) {
  if ([string]::IsNullOrWhiteSpace($tag)) { return $true } # first release
  $args = @("diff","--quiet",$tag,"--")
  $args += $paths
  $psi = New-Object System.Diagnostics.ProcessStartInfo
  $psi.FileName = "git"
  $psi.Arguments = $args -join " "
  $psi.RedirectStandardError = $true
  $psi.RedirectStandardOutput = $true
  $psi.UseShellExecute = $false
  $p = [System.Diagnostics.Process]::Start($psi)
  $p.WaitForExit()
  # exit code 0 = no changes, 1 = changes
  return ($p.ExitCode -ne 0)
}

# --- Start ---
Assert-Tool git
Assert-Tool dotnet
Repo-Root

# Clean tree
if ((git status --porcelain) -ne "") {
  throw "Working tree not clean. Commit or stash first."
}

git fetch --tags | Out-Null

$lastTag = Last-Tag
Write-Host "Last tag:" ($lastTag ?? "<none>")

$changed = Has-Changes-Since $lastTag @("src/OverlayApp","src/OverlayApp.SDK")
if (-not $changed) {
  Write-Host "No changes under src/OverlayApp or src/OverlayApp.SDK since $lastTag. Nothing to release."
  exit 0
}

$current = Get-CurrentVersion $ProjectPath
Write-Host "Current version in csproj:" $current

# Bump menu
Write-Host ""
Write-Host "Select version bump:"
Write-Host "  1) major   (X+1.0.0)"
Write-Host "  2) minor   (Y+1)"
Write-Host "  3) patch   (Z+1)"
Write-Host "  4) keep    (use $current)"
$choice = Read-Host "Enter 1/2/3/4"
$bump = switch ($choice) {
  "1" { "major" }
  "2" { "minor" }
  "3" { "patch" }
  default { "keep" }
}
$next = if ($bump -eq "keep") { $current } else { SemVer-Bump $current $bump }

# Commit message + release notes
$defaultCommit = "chore(release): v$next"
$commitMsg = Read-Host "Commit message [`$default: $defaultCommit`]"
if ([string]::IsNullOrWhiteSpace($commitMsg)) { $commitMsg = $defaultCommit }
$notes = Read-Host "Release notes (optional, one-liner or leave blank)"

Write-Host ""
Write-Host "Summary:"
Write-Host "  Last tag:      " ($lastTag ?? "<none>")
Write-Host "  Version:       " $current "->" $next
Write-Host "  Commit message:" $commitMsg
Write-Host "  Publish dir:   " $PublishDir
Write-Host "  Zip out:       " $DistDir\OverlayApp-$next.zip
$confirm = Read-Host "Proceed to build and release? (Y/N)"
if ($confirm -notin @("Y","y")) { Write-Host "Aborted."; exit 0 }

# Build (publish)
if (Test-Path $PublishDir) { Remove-Item -Recurse -Force $PublishDir }
New-Item -ItemType Directory -Force -Path $PublishDir | Out-Null

if ($PubXml) {
  dotnet publish $ProjectPath -c Release /p:PublishProfile="$PubXml"
} else {
  dotnet publish $ProjectPath -c Release -o $PublishDir
}

# Zip
New-Item -ItemType Directory -Force -Path $DistDir | Out-Null
$zipPath = Join-Path $DistDir ("OverlayApp-" + $next + ".zip")
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Compress-Archive -Path (Join-Path $PublishDir "*") -DestinationPath $zipPath

# Version bump (write, commit, tag, push)
if ($next -ne $current) {
  Set-Version $ProjectPath $next
  git add $ProjectPath
}
git commit -m "$commitMsg"
git tag "v$next"
git push
git push origin "v$next"

# Release (if gh exists)
if (Get-Command gh -ErrorAction SilentlyContinue) {
  $args = @("release","create","v$next",$zipPath,"-t","OverlayApp $next")
  if (-not [string]::IsNullOrWhiteSpace($notes)) {
    $args += @("-n",$notes)
  }
  & gh @args
  Write-Host "Release created with asset: $zipPath"
} else {
  Write-Host "gh not found; upload $zipPath manually to a new GitHub release 'v$next'."
}
