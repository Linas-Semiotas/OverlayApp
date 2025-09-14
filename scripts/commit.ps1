$ErrorActionPreference = "Stop"

function Exit-WithMessage($msg, $color = "Red", [int]$code = 1) {
  Write-Host $msg -ForegroundColor $color
  Pause-AnyKey
  exit $code
}

function Pause-AnyKey {
  try {
    Write-Host ""
    Write-Host "Press any key to close ..."
    $null = $Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown')
  } catch {
    # Fallback if RawUI isn't available (rare)
    cmd /c pause >$null
  }
}

# 1) Check git
if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
  Exit-WithMessage "git not found in PATH."
}

# 2) Ensure we are inside a repo; jump to root
try {
  $repoRoot = git rev-parse --show-toplevel 2>$null
} catch {
  Exit-WithMessage "Not inside a git repository."
}
Set-Location $repoRoot
$branch = (git rev-parse --abbrev-ref HEAD).Trim()

# 3) Pick TYPE
$types = @(
  @{ Key = 'feat';     Desc = 'New end-user feature (UI/module/API)'}
  @{ Key = 'fix';      Desc = 'Bug fix (no breaking change)'}
  @{ Key = 'docs';     Desc = 'Docs only (README/guides)'}
  @{ Key = 'style';    Desc = 'Formatting/linting; no logic change'}
  @{ Key = 'refactor'; Desc = 'Restructure; behavior unchanged'}
  @{ Key = 'perf';     Desc = 'Performance improvement'}
  @{ Key = 'test';     Desc = 'Add/update tests only'}
  @{ Key = 'build';    Desc = 'Build system/deps (csproj, NuGet)'}
  @{ Key = 'ci';       Desc = 'CI/workflows (GitHub Actions)'}
  @{ Key = 'chore';    Desc = 'Maintenance: bumps, cleanup, tooling'}
  @{ Key = 'revert';   Desc = 'Revert a previous commit'}
)
Write-Host ""
Write-Host "Pick commit TYPE:" -ForegroundColor Cyan
for ($i=0; $i -lt $types.Count; $i++) {
  "{0,2}. {1,-9} - {2}" -f ($i+1), $types[$i].Key, $types[$i].Desc | Write-Host
}
$typeIdx = Read-Host "Type #"
if (-not ($typeIdx -as [int]) -or [int]$typeIdx -lt 1 -or [int]$typeIdx -gt $types.Count) {
  Exit-WithMessage "Invalid type selection."
}
$type = $types[[int]$typeIdx-1].Key

# 4) Pick SCOPE (auto from top-level dirs)
$children = Get-ChildItem -LiteralPath $repoRoot -Directory |
            Where-Object { $_.Name -notin @('.git', '.github') } |
            Select-Object -ExpandProperty Name
$scopeList = @('all') + $children
Write-Host ""
Write-Host "Pick commit SCOPE (what area changed):" -ForegroundColor Cyan
for ($i=0; $i -lt $scopeList.Count; $i++) {
  "{0,2}. {1}" -f ($i+1), $scopeList[$i] | Write-Host
}
Write-Host "  0. none"
$scopeIdx = Read-Host "Scope #"
$scopeToken = ""
if ($scopeIdx -and ($scopeIdx -as [int]) -ge 1) {
  $n = [int]$scopeIdx
  if ($n -eq 1)      { $scopeToken = "(all)" }
  elseif ($n -le $scopeList.Count) { $scopeToken = "($($scopeList[$n-1]))" }
} elseif ($scopeIdx -eq '0') {
  $scopeToken = ""
}

# 5) Messages
$summary = Read-Host "Short message (<=72 chars)"
$body    = Read-Host "Optional body (empty to skip)"

$preview = "${type}${scopeToken}: ${summary}"

Write-Host "`nPreview:" -ForegroundColor Cyan
Write-Host $preview -ForegroundColor Yellow
if ($body) { Write-Host $body }

$ok = Read-Host "Proceed? Y/N"
if ($ok -notin @('y','Y')) { Write-Host "Aborted."; Pause-AnyKey; exit 0 }

# 6) Add/commit/push
git add -A

if ($body) {
  git commit -m "$preview" -m "$body"
} else {
  git commit -m "$preview"
}

git push -u origin $branch
Write-Host "`nDone." -ForegroundColor Green
Pause-AnyKey
exit   #