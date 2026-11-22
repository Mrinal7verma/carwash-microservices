# run-all.ps1: Starts all 6 CarWash microservices in background jobs, logs to logs/, and cleanly stops on Ctrl+C.
$ErrorActionPreference = "Stop"

$RootDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $RootDir

$LogsDir = Join-Path $RootDir "logs"
if (-not (Test-Path $LogsDir)) {
    New-Item -ItemType Directory -Path $LogsDir | Out-Null
}

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " Starting CarWash Microservices System (.NET 10)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

Write-Host "Building solution up-front to prevent DLL file lock collisions..." -ForegroundColor Cyan
dotnet build --no-restore
if ($LASTEXITCODE -ne 0) {
    throw "Build failed. Please resolve build errors before launching."
}

$Processes = [System.Collections.Generic.List[System.Diagnostics.Process]]::new()

function Stop-AllServices {
    Write-Host "`nShutting down all CarWash services..." -ForegroundColor Yellow
    foreach ($proc in $Processes) {
        if (-not $proc.HasExited) {
            try {
                $proc.Kill($true)
            } catch {
                # Ignore process kill errors during shutdown
            }
        }
    }
    Write-Host "All CarWash services stopped cleanly." -ForegroundColor Green
}

try {
    Write-Host "1/6 Starting NotificationService (Internal, port 5005)..."
    $p1 = Start-Process dotnet -ArgumentList "run --project src/NotificationService/NotificationService.csproj --no-build" -RedirectStandardOutput "$LogsDir\notificationservice.log" -RedirectStandardError "$LogsDir\notificationservice.err.log" -PassThru -NoNewWindow
    $Processes.Add($p1)

    Write-Host "2/6 Starting AuthService (Port 5001)..."
    $p2 = Start-Process dotnet -ArgumentList "run --project src/AuthService/AuthService.csproj --no-build" -RedirectStandardOutput "$LogsDir\authservice.log" -RedirectStandardError "$LogsDir\authservice.err.log" -PassThru -NoNewWindow
    $Processes.Add($p2)

    Write-Host "3/6 Starting CustomerService (Port 5002)..."
    $p3 = Start-Process dotnet -ArgumentList "run --project src/CustomerService/CustomerService.csproj --no-build" -RedirectStandardOutput "$LogsDir\customerservice.log" -RedirectStandardError "$LogsDir\customerservice.err.log" -PassThru -NoNewWindow
    $Processes.Add($p3)

    Write-Host "4/6 Starting WasherService (Port 5003)..."
    $p4 = Start-Process dotnet -ArgumentList "run --project src/WasherService/WasherService.csproj --no-build" -RedirectStandardOutput "$LogsDir\washerservice.log" -RedirectStandardError "$LogsDir\washerservice.err.log" -PassThru -NoNewWindow
    $Processes.Add($p4)

    Write-Host "5/6 Starting BookingService (Port 5004)..."
    $p5 = Start-Process dotnet -ArgumentList "run --project src/BookingService/BookingService.csproj --no-build" -RedirectStandardOutput "$LogsDir\bookingservice.log" -RedirectStandardError "$LogsDir\bookingservice.err.log" -PassThru -NoNewWindow
    $Processes.Add($p5)

    Start-Sleep -Seconds 4

    Write-Host "6/6 Starting ApiGateway (YARP Reverse Proxy, port 5000)..."
    $p6 = Start-Process dotnet -ArgumentList "run --project src/ApiGateway/ApiGateway.csproj --no-build" -RedirectStandardOutput "$LogsDir\apigateway.log" -RedirectStandardError "$LogsDir\apigateway.err.log" -PassThru -NoNewWindow
    $Processes.Add($p6)

    Write-Host "==========================================================" -ForegroundColor Green
    Write-Host " All services running! Logs available in logs\*.log" -ForegroundColor Green
    Write-Host " Gateway URL: http://localhost:5000" -ForegroundColor Green
    Write-Host " Press Ctrl+C to terminate all services." -ForegroundColor Green
    Write-Host "==========================================================" -ForegroundColor Green

    while ($true) {
        Start-Sleep -Seconds 1
    }
} finally {
    Stop-AllServices
}
