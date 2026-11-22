# On-Demand Car Wash System - Backend Microservices (.NET 10)

A distributed, on-demand car wash platform built with .NET 10 microservices, SQL Server, Entity Framework Core, YARP (Yet Another Reverse Proxy), Serilog, MailKit, and BCrypt authentication.

---

## Architecture Overview

The system consists of 6 dedicated services following a database-per-service pattern with no cross-database foreign keys. Cross-service entity references use logical `BIGINT` IDs, and inter-service communication is conducted synchronously via resilient typed `HttpClient` instances.

```
                    +---------------------------+
                    | Client / Frontend / Curl  |
                    +-------------+-------------+
                                  |
                                  v
                    +---------------------------+
                    |  ApiGateway (YARP: 5000)  |
                    |  - CORS & Rate Limiting   |
                    |  - 404 on /internal/*     |
                    |  - CorrelationId Tracking |
                    +----+-----+-----+-----+----+
                         |     |     |     |
         +---------------+     |     |     +---------------+
         |                     |     |                     |
         v                     v     v                     v
+-----------------+   +--------------+   +---------------+   +----------------+
|   AuthService   |   |CustomerService|  | WasherService |   | BookingService |
|   (Port 5001)   |   | (Port 5002)  |   |  (Port 5003)  |   |  (Port 5004)   |
|   - Stateless   |   | - Customers  |   |  - Washers    |   |  - Packages    |
|   - Admin Auth  |   | - Cars       |   |  - Profiles   |   |  - Bookings    |
|   - JWT Issuer  |   |              |   |               |   |  - Cleanup Job |
+-----------------+   +-------+------+   +-------+-------+   +---+---+----+---+
                              ^                  ^               |   |    |
                              |                  |               |   |    |
                              +------------------+---------------+   |    |
                                      Typed HttpClients              |    |
                                                                     |    v
                                                             +-------+--------+
                                                             |NotificationSvc |
                                                             |  (Port 5005)   |
                                                             |  - Smtp/MailKit|
                                                             |  - InternalOnly|
                                                             +----------------+
```

### Microservices Summary
- **ApiGateway (Port 5000):** Central reverse proxy powered by YARP. Enforces IP-based rate limiting (100 req/min), CORS policies, Correlation ID propagation, and blocks external access to `/internal/*` routes.
- **AuthService (Port 5001):** Stateless authentication provider. Authenticates administrator credentials and issues signed JWT tokens.
- **CustomerService (Port 5002):** Manages customer registrations, logins, profiles, and vehicles (`CarWash_CustomerDb`).
- **WasherService (Port 5003):** Manages washer registrations, logins, profiles, and availability toggles (`CarWash_WasherDb`).
- **BookingService (Port 5004):** Core domain service managing service packages, booking lifecycle (Pending, Accepted, Completed, Cancelled, Expired), washer assignments, concurrency resolution, and background expiry cleanup (`CarWash_BookingDb`).
- **NotificationService (Port 5005):** Internal-only email dispatch service via MailKit/SMTP. Bypasses actual email transmission in Development mode.

---

## Ports & Services Table

| Service | Port | Database | External Route (via Gateway 5000) | Swagger UI (Direct) |
| :--- | :--- | :--- | :--- | :--- |
| **ApiGateway** | `5000` | N/A | Entrypoint for all external calls | N/A |
| **AuthService** | `5001` | N/A (Stateless) | `/api/auth/**` | `http://localhost:5001/swagger` |
| **CustomerService** | `5002` | `CarWash_CustomerDb` | `/api/customers/**` | `http://localhost:5002/swagger` |
| **WasherService** | `5003` | `CarWash_WasherDb` | `/api/washers/**` | `http://localhost:5003/swagger` |
| **BookingService** | `5004` | `CarWash_BookingDb` | `/api/bookings/**` | `http://localhost:5004/swagger` |
| **NotificationService** | `5005` | N/A (Internal) | *Not Routed (Internal Only)* | N/A (Internal API) |

---

## Prerequisites

- **.NET 10 SDK** (v10.0 or higher)
- **dotnet-ef tool** (`dotnet tool install --global dotnet-ef` or version 10.x)
- **Docker / Docker Desktop** (for SQL Server container)

---

## Starting SQL Server

The microservices require Microsoft SQL Server 2022 running on port 1433 with the SA password `Your_Strong_Pass123`.

### Option 1: Docker CLI
```bash
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=Your_Strong_Pass123" \
   -p 1433:1433 --name carwash-sql --platform linux/amd64 -d \
   mcr.microsoft.com/mssql/server:2022-latest
```

### Option 2: Docker Compose
```bash
docker compose up -d
```

> **Note on Migrations:** In Development mode, `CustomerService`, `WasherService`, and `BookingService` automatically apply their respective EF Core database migrations on startup with an automatic retry loop (~30 seconds). No manual `dotnet ef database update` is required.

---

## Running the Services

### Option 1: Start All Services via Script (Recommended)

#### On macOS / Linux:
```bash
./run-all.sh
```
*(Starts all 6 projects in the background, logs output to `logs/*.log`, and shuts down all processes on `Ctrl+C`)*

#### On Windows (PowerShell):
```powershell
.\run-all.ps1
```

### Option 2: Run Services Individually
Run each service in a separate terminal:
```bash
# Terminal 1: NotificationService
dotnet run --project src/NotificationService/NotificationService.csproj

# Terminal 2: AuthService
dotnet run --project src/AuthService/AuthService.csproj

# Terminal 3: CustomerService
dotnet run --project src/CustomerService/CustomerService.csproj

# Terminal 4: WasherService
dotnet run --project src/WasherService/WasherService.csproj

# Terminal 5: BookingService
dotnet run --project src/BookingService/BookingService.csproj

# Terminal 6: ApiGateway
dotnet run --project src/ApiGateway/ApiGateway.csproj
```

---

## Default Admin Credentials

- **Email:** `admin@carwash.com`
- **Password:** `Admin@123`
- **Role:** `Admin`
- **Login Endpoint:** `POST http://localhost:5000/api/auth/admin/login`

---

## Testing & API Documentation

- **HTTP Requests:** See [CarWash.http](CarWash.http) for runnable request samples covering every endpoint through the API Gateway on port 5000.
- **Unit Tests:** Execute all tests with:
  ```bash
  dotnet test CarWash.sln
  ```

---

## Assumptions

- .NET 10.0 is used across all solution projects with C# Nullable reference types and Implicit Usings enabled.
- Central code quality analyzers (StyleCop.Analyzers and Microsoft.CodeAnalysis.NetAnalyzers) are configured via `Directory.Build.props` as warnings only (`TreatWarningsAsErrors=false`).
- Each service runs HTTP only on assigned localhost ports: ApiGateway (5000), AuthService (5001), CustomerService (5002), WasherService (5003), BookingService (5004), NotificationService (5005).
- Cross-service communication uses typed `HttpClient` instances with `CorrelationIdHandler` forwarding the `X-Correlation-Id` header.
- Internal-only endpoints (such as NotificationService and internal validation routes) authenticate requests using the `X-Internal-Api-Key` header matching `InternalApi:Key`.
- JWT authentication uses HMAC SHA256 validation with symmetric key configured under `Jwt:Key`, issuer under `Jwt:Issuer`, audience under `Jwt:Audience`, and expiry under `Jwt:ExpiryMinutes`.
- Error responses uniformly follow `{ "message": "...", "correlationId": "..." }`.
- Serilog writes daily rolling logs under `logs/<service>-.log` and to the console, masking sensitive parameters (passwords, tokens, keys).
- AuthService is stateless without a database; the admin user is verified against configured `Admin:Email` and `Admin:PasswordHash` (BCrypt) and issued a token with role `Admin` and logical user ID `1`.
- NotificationService is an internal-only service with no JWT authentication; when `Smtp:Enabled` is false (default in Development), email dispatch is bypassed and safely logged with only `to` and `subject`.
- Configuration files follow standard precedence (`appsettings.json`, `appsettings.{Environment}.json`, Environment variables). Secrets in `appsettings.Development.json` are marked for development use only.
- In CustomerService and WasherService, database migrations run on startup in Development with automated retries (up to 30 seconds) to accommodate SQL Server container initialization.
- Added endpoint `PUT /api/washers/availability` in WasherService enabling authenticated washers to toggle their availability status (`isAvailable`), returning an updated status message.
- Typed `BookingClient` uses `Microsoft.Extensions.Http.Resilience` with up to 2 retries and short backoff exclusively for idempotent HTTP GET calls, mapping 5xx or communication errors to `ServiceUnavailableException` (503).
- BookingService connects to `CarWash_BookingDb` with automatic EF Core startup migrations in Development with a 30-second retry loop.
- Service packages are seeded with two active packages: "Basic Wash" (ID 1, 299.00, 30 min) and "Full Interior & Exterior" (ID 2, 599.00, 90 min).
- Logical cross-service IDs (`CustomerId`, `CarId`, `WasherId`) are stored as plain `BIGINT` without cross-database foreign keys.
- Washer status update endpoint accepts ONLY "Accepted", "Rejected", or "Completed" (any other value, including verb forms, returns 400 Bad Request).
- Booking acceptance employs an atomic conditional `ExecuteUpdateAsync` query against `Status == BookingStatus.Pending` to prevent concurrent acceptance races; if 0 rows are affected (already claimed or no longer pending), it returns 400 Bad Request (`BadRequestException`).
- Washer rejections are stored in `BookingRejections` with a unique index on `(BookingId, WasherId)` to prevent duplicate rejections while keeping the booking available for other washers.
- Customer booking cancellation allows `Pending` AND `Accepted` bookings to transition to `Cancelled` (200 OK). Bookings in `Completed`, `Cancelled`, or `Expired` status return 409 Conflict (`ConflictException`). If the booking is not found or not owned by the caller, 404 Not Found is returned.
- Customer vehicle endpoints are hosted under `/api/customers/cars` (e.g. `GET /api/customers/cars/{carId}` requiring Customer role and returning 404 if not found or not owned). `CustomerClient` in BookingService calls `GET /api/customers/cars/{carId}` forwarding the caller's JWT and `GET /api/customers/internal/{customerId}/email` with `X-Internal-Api-Key` returning `{"email":"..."}`. Routes matching `/api/cars/**` are removed from the gateway.
- Email notifications are non-blocking for booking operations and run within a dedicated `IServiceScopeFactory` scope without accessing `HttpContext` after the request terminates. Failures are caught and logged at Error level without affecting booking responses.
- `PendingBookingCleanupService` runs as a background service on a configurable interval (`Cleanup:IntervalMinutes`, default 60) to automatically expire pending bookings whose scheduled time has passed.
- ApiGateway uses YARP 2.3.0 reverse proxy routing external requests on port 5000 to backend services.
- `InternalEndpointBlockingMiddleware` intercepts any request containing `/internal/` and immediately terminates the pipeline with a 404 Not Found response and standard `ErrorResponse` payload.
- ApiGateway applies ASP.NET Core built-in rate limiting with a fixed window of 100 requests per minute partitioned by client IP address (`Microsoft.AspNetCore.RateLimiting`), returning 429 Too Many Requests when exceeded.
- CORS is globally configured for `http://localhost:4200`, `http://localhost:3000`, and `http://localhost:5173`.
- `run-all.sh` and `run-all.ps1` manage process lifetimes, write stdout/stderr to `logs/`, and cleanly terminate child microservice processes on SIGINT/Ctrl+C.
- Unit testing uses xUnit, Moq, and Coverlet. CustomerService, WasherService, AuthService, and NotificationService use EF Core InMemory; BookingService uses SQLite in-memory to support `ExecuteUpdateAsync` and unique constraint enforcement. Code coverage exceeds 80% line coverage across all services.
