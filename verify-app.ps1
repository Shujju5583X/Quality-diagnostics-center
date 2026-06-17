<#
.SYNOPSIS
  Prerequisite checker for Quality Diagnostics Center - LabSystem WPF application.
.DESCRIPTION
  Checks that the target laptop meets minimum requirements to run the application.
  Tests: .NET Framework version, Windows version, disk space, and published EXE.
#>

$ErrorActionPreference = "Stop"

$pass = 0
$fail = 0
$warn = 0

function Write-Result($label, $status, $detail) {
    $symbol = switch ($status) {
        "PASS"  { "[PASS]" }
        "FAIL"  { "[FAIL]" }
        "WARN"  { "[WARN]" }
    }
    $color = switch ($status) {
        "PASS"  { "Green" }
        "FAIL"  { "Red" }
        "WARN"  { "Yellow" }
    }
    Write-Host ("{0,-8} {1,-45} {2}" -f $symbol, $label, $detail) -ForegroundColor $color
}

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  Quality Diagnostics Center - Prerequisites" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# -------- Check 1: .NET Framework 4.5.1+ --------
$dotnetOk = $false
$dotnetVersion = ""
try {
    $release = Get-ItemPropertyValue -Path "HKLM:SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" -Name Release -ErrorAction Stop
    if ($release -ge 378389) { $dotnetVersion = "4.5.1+"; $dotnetOk = $true }
    if ($release -ge 379893) { $dotnetVersion = "4.5.2+" }
    if ($release -ge 393295) { $dotnetVersion = "4.6+" }
    if ($release -ge 394254) { $dotnetVersion = "4.6.1+" }
    if ($release -ge 394802) { $dotnetVersion = "4.6.2+" }
    if ($release -ge 460798) { $dotnetVersion = "4.7+" }
    if ($release -ge 461308) { $dotnetVersion = "4.7.1+" }
    if ($release -ge 461808) { $dotnetVersion = "4.7.2+" }
    if ($release -ge 528040) { $dotnetVersion = "4.8+" }
    if ($release -ge 533320) { $dotnetVersion = "4.8.1+" }
} catch {
    $dotnetVersion = "Not detected"
}
if ($dotnetOk) {
    Write-Result ".NET Framework" "PASS" "$dotnetVersion (release: $release)"
    $pass++
} else {
    Write-Result ".NET Framework" "FAIL" "4.5.1+ required — detected: $dotnetVersion"
    $fail++
}

# -------- Check 2: Windows Version --------
$os = Get-CimInstance Win32_OperatingSystem
$osBuild = [int]($os.BuildNumber)
$osVersion = "$($os.Caption) (Build $($os.BuildNumber))"

if ($osBuild -ge 7601) {
    Write-Result "Windows Version" "PASS" $osVersion
    $pass++
} else {
    Write-Result "Windows Version" "FAIL" "Windows 7 SP1+ required — $osVersion"
    $fail++
}

# -------- Check 3: Disk Space --------
$drive = Get-PSDrive -Name ($os.SystemDrive[0]) -ErrorAction SilentlyContinue
if ($drive) {
    $freeMB = [math]::Round($drive.Free / 1MB)
    if ($freeMB -ge 200) {
        Write-Result "Free Disk Space" "PASS" "$freeMB MB free on $($drive.Root) (min 200 MB)"
        $pass++
    } else {
        Write-Result "Free Disk Space" "FAIL" "Only $freeMB MB free on $($drive.Root) — need >= 200 MB"
        $fail++
    }
} else {
    Write-Result "Free Disk Space" "WARN" "Could not check"
    $warn++
}

# -------- Check 4: Published EXE --------
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$exePath = Join-Path $scriptPath "LabSystem.UI.exe"
if (Test-Path $exePath) {
    $exeInfo = Get-Item $exePath
    $sizeMB = [math]::Round($exeInfo.Length / 1MB, 2)
    Write-Result "Application EXE" "PASS" "$($exeInfo.Name) ($sizeMB MB)"
    $pass++
} else {
    Write-Result "Application EXE" "FAIL" "Not found at $exePath"
    Write-Result "Hint" "WARN" "Run publish.bat first to generate the build output"
    $fail++
}

# -------- Summary --------
Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
$total = $pass + $fail + $warn
Write-Host "  Results: $pass passed, $fail failed, $warn warnings" -ForegroundColor $(if ($fail -gt 0) { "Red" } elseif ($warn -gt 0) { "Yellow" } else { "Green" })
Write-Host "============================================" -ForegroundColor Cyan

if ($fail -gt 0) {
    Write-Host ""
    Write-Host "Some checks FAILED. Please resolve the issues above before running the application." -ForegroundColor Red
    exit 1
} else {
    Write-Host "All checks passed! You can run LabSystem.UI.exe" -ForegroundColor Green
    exit 0
}
