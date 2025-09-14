$ErrorActionPreference = 'Stop'

function Require-Tool([string]$name) {
    if (-not (Get-Command $name -ErrorAction SilentlyContinue)) {
        Write-Host "$name not found in PATH. Please install it." -ForegroundColor Red
        exit 1
    }
}

function Coalesce([string]$a, [string]$b) {
    if ([string]::IsNullOrWhiteSpace($a)) { $b } else { $a }
}

# Return $true if there are local changes OUTSIDE the scripts/ folder
function IsDirtyExcludingScripts {
    $changes = & git status --porcelain
    if (-not $changes) { return $false }
    $lines = $changes -split "`r?`n" | Where-Object { $_ -ne "" }
    foreach ($l in $lines) {
        # format: "XY path"
        $path = $l.Substring([Math]::Min(3, $l.Length)).Trim()
        if ($path -and -not ($path -like "scripts/*" -or $path -like "scripts\*")) {
            return $true
        }
    }
    return $false
}

# --- Tool checks ---
Require-Tool git
Require-Tool dotnet
$GhAvailable = $null -ne (Get-Command gh -ErrorAction SilentlyContinue)

# --- Repo root ---
try { $root = (git rev-parse --show-toplevel 2>$null).Trim() } catch { $root = $null }
if (-not $root) { Write-Host "Not inside a git repo." -ForegroundColor Red; exit 1 }
Set-Location $root

# --- Branch info ---
try { $branch = (git rev-parse --abbrev-ref HEAD).Trim() } catch { $branch = "unknown" }

# --- Remote state (preview only for now) ---
& git fetch --prune --tags *> $null
$localSha  = (git rev-parse HEAD).Trim()
$remoteSha = (git rev-parse "origin/$branch" 2>$null).Trim()
$counts = (git rev-list --left-right --count HEAD...origin/$branch 2>$null)
$ahead = "0"; $behind = "0"
if ($counts) { $parts = $counts -split "\s+"; if ($parts.Count -ge 2) { $ahead = $parts[0]; $behind = $parts[1] } }

# --- Find app csproj ---
$appCsproj = Join-Path $root 'src\OverlayApp\OverlayApp.csproj'
if (-not (Test-Path $appCsproj)) {
    $candidate = Get-ChildItem -Recurse -Filter 'OverlayApp.csproj' | Select-Object -First 1
    if ($null -eq $candidate) { Write-Host "OverlayApp.csproj not found." -ForegroundColor Red; exit 1 }
    $appCsproj = $candidate.FullName
}

# --- Read current version from csproj ---
$csprojText = Get-Content $appCsproj -Raw
$verRe     = '<Version>\s*v?([0-9]+(?:\.[0-9]+){0,2})\s*</Version>'
$verMatch  = [regex]::Match($csprojText, $verRe, 'IgnoreCase')
$currentVer = $(if ($verMatch.Success) { $verMatch.Groups[1].Value } else { '0.1.0' })

# --- Last local tag ---
try { $lastTag = (git describe --tags --abbrev=0 2>$null).Trim() } catch { $lastTag = $null }
$lastTagDisplay = Coalesce $lastTag '<none>'

# --- Latest GitHub Release tag (optional) ---
$latestReleaseTag = '<n/a>'
if ($GhAvailable) {
    $rl = & gh release list --limit 1 2>$null
    if ($LASTEXITCODE -eq 0 -and $rl) { $latestReleaseTag = ($rl -split '\s+')[0] } else { $latestReleaseTag = '<none>' }
}

# --- Preview header ---
Write-Host "Branch:               $branch"
Write-Host "Local vs origin:      ahead=$ahead  behind=$behind"
Write-Host "Working tree dirty*:  $(if (IsDirtyExcludingScripts) {'YES'} else {'no'})  (*ignores scripts/)"
Write-Host "Current csproj ver:   $currentVer"
Write-Host "Last local git tag:   $lastTagDisplay"
Write-Host "Latest GH release:    $latestReleaseTag"
Write-Host ""

# Heads-up on mismatches (preview only)
if ($lastTag -and $lastTag -ne "v$currentVer") {
    Write-Host "NOTE: csproj ($currentVer) != last tag ($lastTag)." -ForegroundColor Yellow
}

# --- Choose bump ---
Write-Host "Select version action:"
Write-Host "0. keep (no bump) -> $currentVer"
$parts = @(); try { $parts = $currentVer.Split('.') } catch { $parts = @('0','1','0') }
while ($parts.Count -lt 3) { $parts += '0' }
[int]$maj = $parts[0]; [int]$min = $parts[1]; [int]$pat = $parts[2]
Write-Host "1. major -> $($maj+1).0.0"
Write-Host "2. minor -> $maj.$($min+1).0"
Write-Host "3. patch -> $maj.$min.$($pat+1)"
$choice = Read-Host "Choice (0/1/2/3)"
if ($choice -notin @('0','1','2','3')) { Write-Host "Aborted."; exit 0 }

$updateCsproj = $true
switch ($choice) {
    '0' { $newVer = $currentVer; $updateCsproj = $false }
    '1' { $newVer = "$($maj+1).0.0" }
    '2' { $newVer = "$maj.$($min+1).0" }
    '3' { $newVer = "$maj.$min.$($pat+1)" }
}

# --- Plan summary ---
Write-Host ""
Write-Host "Plan:"
if ($updateCsproj) { Write-Host "  - Bump csproj: $currentVer -> $newVer" } else { Write-Host "  - Keep version: $currentVer (rebuild/release)" }
Write-Host "  - dotnet publish  -> publish\OverlayApp"
Write-Host "  - Zip artifact    -> publish\OverlayApp-v$newVer.zip"
Write-Host "  - Commit/tag/push -> only at the very end (after another confirm)"
Write-Host ""

# First confirm (no git writes yet)
$go = Read-Host "Proceed with publish/zip? (Y/N)"
if ($go -notmatch '^(y|Y)$') { Write-Host "Aborted."; exit 0 }

# --- Update csproj if bumping ---
if ($updateCsproj) {
    if ($verMatch.Success) {
        $csprojText = [regex]::Replace($csprojText, $verRe, "<Version>$newVer</Version>", 'IgnoreCase')
    } else {
        $csprojText = $csprojText -replace '</PropertyGroup>', "  <Version>$newVer</Version>`r`n</PropertyGroup>"
    }
    Set-Content -Path $appCsproj -Value $csprojText -Encoding UTF8
    Write-Host "Updated $appCsproj to $newVer"
}

# --- Publish ---
$publishDir = Join-Path $root 'publish\OverlayApp'
if (-not (Test-Path $publishDir)) { New-Item -ItemType Directory -Path $publishDir | Out-Null }
Write-Host "Publishing to $publishDir ..."
dotnet publish $appCsproj -c Release -o $publishDir

# --- Zip output ---
$zipName = "OverlayApp-v$newVer.zip"
$zipPath = Join-Path $root ("publish\" + $zipName)
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Write-Host "Creating $zipName ..."
Compress-Archive -Path (Join-Path $publishDir '*') -DestinationPath $zipPath -Force

# --- Final checks & confirmation BEFORE any git writes ---
$dirty = IsDirtyExcludingScripts
$finalWarns = @()
if ($dirty) { $finalWarns += "Working tree has changes outside scripts/." }
if ($behind -ne '0') { $finalWarns += "Your branch is BEHIND origin/$branch ($behind). Run: git pull --ff-only" }

if ($finalWarns.Count -gt 0) {
    Write-Host ""
    Write-Host "Pre-flight warnings:" -ForegroundColor Yellow
    $finalWarns | ForEach-Object { Write-Host "  • $_" -ForegroundColor Yellow }
    Write-Host ""
}

$mode = Read-Host "Continue: [F]ull (commit/tag/push/release), [P]ublish only (no git), [A]bort?"
switch -regex ($mode) {
    '^[Pp]$' { Write-Host "Publish-only complete. Zip at: $zipPath"; exit 0 }
    '^[Aa]$' { Write-Host "Aborted."; exit 0 }
    default  { } # Full path continues
}

# Block if behind remote (to avoid non-FF push)
if ($behind -ne '0') {
    Write-Host "Blocked: local is behind origin/$branch. Do: git pull --ff-only" -ForegroundColor Red
    exit 1
}

# Block if dirty outside scripts/
if ($dirty) {
    Write-Host "Blocked: uncommitted changes outside scripts/." -ForegroundColor Red
    Write-Host "Commit/stash them or move changes under scripts/, then re-run." -ForegroundColor Red
    exit 1
}

# --- Git stage/commit/push (only if version bumped) ---
if ($updateCsproj) {
    git add "$appCsproj"
    git commit -m "build(app): bump version to v$newVer"
    git push -u origin $branch
}

# --- Tag handling ---
$tagName = "v$newVer"
$tagExistsLocal = (git tag -l $tagName)
if (-not $tagExistsLocal) { git tag $tagName }
try { git push origin $tagName } catch { }

# --- GitHub release handling ---
if ($GhAvailable) {
    & gh release view $tagName 1>$null 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Release $tagName exists; updating asset..."
        gh release upload $tagName "$zipPath" --clobber
    } else {
        Write-Host "Creating release $tagName ..."
        gh release create $tagName "$zipPath" --title "OverlayApp $tagName" --notes "Release $tagName"
    }
} else {
    Write-Host "gh not found; upload $zipPath manually on the Releases page."
}

Write-Host "Done."