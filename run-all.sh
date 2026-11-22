#!/usr/bin/env bash
# run-all.sh: Starts all 6 CarWash microservices in the background, logs output to logs/, and cleanly stops on Ctrl+C.
set -e

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$ROOT_DIR"

export DOTNET_CLI_HOME="${DOTNET_CLI_HOME:-/tmp}"
mkdir -p logs

echo "=========================================================="
echo " Starting CarWash Microservices System (.NET 10)"
echo "=========================================================="

PIDS=()

cleanup() {
    echo ""
    echo "Shutting down all CarWash services..."
    for pid in "${PIDS[@]}"; do
        if kill -0 "$pid" 2>/dev/null; then
            kill "$pid" 2>/dev/null || true
        fi
    done
    wait 2>/dev/null || true
    echo "All CarWash services stopped cleanly."
}

trap cleanup INT TERM EXIT

echo "1/6 Starting NotificationService (Internal, port 5005)..."
dotnet run --project src/NotificationService/NotificationService.csproj > logs/notificationservice.log 2>&1 &
PIDS+=($!)

echo "2/6 Starting AuthService (Port 5001)..."
dotnet run --project src/AuthService/AuthService.csproj > logs/authservice.log 2>&1 &
PIDS+=($!)

echo "3/6 Starting CustomerService (Port 5002)..."
dotnet run --project src/CustomerService/CustomerService.csproj > logs/customerservice.log 2>&1 &
PIDS+=($!)

echo "4/6 Starting WasherService (Port 5003)..."
dotnet run --project src/WasherService/WasherService.csproj > logs/washerservice.log 2>&1 &
PIDS+=($!)

echo "5/6 Starting BookingService (Port 5004)..."
dotnet run --project src/BookingService/BookingService.csproj > logs/bookingservice.log 2>&1 &
PIDS+=($!)

# Brief pause to allow backend microservices to initialize and apply migrations before gateway starts
sleep 4

echo "6/6 Starting ApiGateway (YARP Reverse Proxy, port 5000)..."
dotnet run --project src/ApiGateway/ApiGateway.csproj > logs/apigateway.log 2>&1 &
PIDS+=($!)

echo "=========================================================="
echo " All services running! Logs available in logs/*.log"
echo " Gateway URL: http://localhost:5000"
echo " Press Ctrl+C to terminate all services."
echo "=========================================================="

wait
