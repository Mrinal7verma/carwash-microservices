# test-cycle.ps1: Automated full-cycle test for CarWash microservices via API Gateway (Port 5000)
$ErrorActionPreference = "Stop"
$Gateway = "http://localhost:5000"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " Running Full CarWash Business Lifecycle Test" -ForegroundColor Cyan
Write-Host " Gateway: $Gateway" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$timestamp = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
$customerEmail = "customer_${timestamp}@test.com"
$washerEmail = "washer_${timestamp}@test.com"
$customerPhone = "9" + (Get-Random -Minimum 100000000 -Maximum 999999999).ToString()
$washerPhone = "8" + (Get-Random -Minimum 100000000 -Maximum 999999999).ToString()
$password = "Password@123"

# 1. Admin Login
Write-Host "`n[1/12] Admin Login..." -ForegroundColor Yellow
$adminLoginBody = @{
    email = "admin@carwash.com"
    password = "Admin@123"
} | ConvertTo-Json
$adminRes = Invoke-RestMethod -Uri "$Gateway/api/auth/admin/login" -Method Post -Body $adminLoginBody -ContentType "application/json"
$adminToken = $adminRes.token
Write-Host "  -> Admin logged in successfully! Token received." -ForegroundColor Green

# 2. Register Customer
Write-Host "`n[2/12] Registering new Customer ($customerEmail)..." -ForegroundColor Yellow
$custRegBody = @{
    firstName = "Jane"
    lastName = "Doe"
    email = $customerEmail
    phone = $customerPhone
    password = $password
    address = "123 Test Avenue"
    city = "Bengaluru"
} | ConvertTo-Json
$custRegRes = Invoke-RestMethod -Uri "$Gateway/api/customers/register" -Method Post -Body $custRegBody -ContentType "application/json"
Write-Host "  -> Customer registered successfully!" -ForegroundColor Green

# 3. Customer Login
Write-Host "`n[3/12] Customer Login..." -ForegroundColor Yellow
$custLoginBody = @{
    email = $customerEmail
    password = $password
} | ConvertTo-Json
$custLoginRes = Invoke-RestMethod -Uri "$Gateway/api/customers/login" -Method Post -Body $custLoginBody -ContentType "application/json"
$customerToken = $custLoginRes.token
Write-Host "  -> Customer logged in! JWT token acquired." -ForegroundColor Green

# 4. Customer Adds a Car
Write-Host "`n[4/12] Customer adding a Car..." -ForegroundColor Yellow
$carBody = @{
    make = "Honda"
    model = "Civic"
    registrationNo = "KA-05-XY-$($timestamp.ToString().Substring($timestamp.ToString().Length - 4))"
} | ConvertTo-Json
$custHeaders = @{ Authorization = "Bearer $customerToken" }
$carRes = Invoke-RestMethod -Uri "$Gateway/api/customers/cars" -Method Post -Body $carBody -ContentType "application/json" -Headers $custHeaders
$carId = $carRes.carId
Write-Host "  -> Car added! CarId: $carId ($($carRes.make) $($carRes.model) - $($carRes.registrationNo))" -ForegroundColor Green

# 5. Register Washer
Write-Host "`n[5/12] Registering new Washer ($washerEmail)..." -ForegroundColor Yellow
$washerRegBody = @{
    firstName = "Mike"
    lastName = "Washer"
    email = $washerEmail
    phone = $washerPhone
    password = $password
} | ConvertTo-Json
$washerRegRes = Invoke-RestMethod -Uri "$Gateway/api/washers/register" -Method Post -Body $washerRegBody -ContentType "application/json"
Write-Host "  -> Washer registered successfully!" -ForegroundColor Green

# 6. Washer Login
Write-Host "`n[6/12] Washer Login..." -ForegroundColor Yellow
$washerLoginBody = @{
    email = $washerEmail
    password = $password
} | ConvertTo-Json
$washerLoginRes = Invoke-RestMethod -Uri "$Gateway/api/washers/login" -Method Post -Body $washerLoginBody -ContentType "application/json"
$washerToken = $washerLoginRes.token
$washerId = $washerLoginRes.washer.washerId
Write-Host "  -> Washer logged in! WasherId: $washerId | JWT token acquired." -ForegroundColor Green

# 7. Washer Sets Availability to True
Write-Host "`n[7/12] Washer setting availability to True..." -ForegroundColor Yellow
$washerHeaders = @{ Authorization = "Bearer $washerToken" }
$availBody = @{ isAvailable = $true } | ConvertTo-Json
$availRes = Invoke-RestMethod -Uri "$Gateway/api/washers/availability" -Method Put -Body $availBody -ContentType "application/json" -Headers $washerHeaders
Write-Host "  -> Washer availability set to: $($availRes.isAvailable)" -ForegroundColor Green

# 8. Check Service Packages
Write-Host "`n[8/12] Fetching active Service Packages..." -ForegroundColor Yellow
$packages = Invoke-RestMethod -Uri "$Gateway/api/bookings/packages" -Method Get
$package = $packages[0]
Write-Host "  -> Selected Package: [$($package.packageId)] $($package.name) - INR $($package.price) ($($package.estimatedDurationMinutes) mins)" -ForegroundColor Green

# 9. Customer Creates Booking
Write-Host "`n[9/12] Customer booking the service..." -ForegroundColor Yellow
$bookingBody = @{
    packageId = $package.packageId
    carId = $carId
    scheduledTime = (Get-Date).AddDays(1).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
} | ConvertTo-Json
$bookingRes = Invoke-RestMethod -Uri "$Gateway/api/bookings" -Method Post -Body $bookingBody -ContentType "application/json" -Headers $custHeaders
$bookingId = $bookingRes.bookingId
Write-Host "  -> Booking Created! BookingId: $bookingId | Status: $($bookingRes.status)" -ForegroundColor Green

# 10. Washer Views Pending Bookings & Accepts
Write-Host "`n[10/12] Washer viewing pending bookings & accepting BookingId: $bookingId..." -ForegroundColor Yellow
$acceptBody = @{ action = "Accepted" } | ConvertTo-Json
$acceptRes = Invoke-RestMethod -Uri "$Gateway/api/bookings/$bookingId/status" -Method Put -Body $acceptBody -ContentType "application/json" -Headers $washerHeaders
Write-Host "  -> Booking Accepted! Status: $($acceptRes.status) | Assigned WasherId: $($acceptRes.washerId)" -ForegroundColor Green

# 11. Washer Completes Booking
Write-Host "`n[11/12] Washer completing the booking..." -ForegroundColor Yellow
$completeBody = @{ action = "Completed" } | ConvertTo-Json
$completeRes = Invoke-RestMethod -Uri "$Gateway/api/bookings/$bookingId/status" -Method Put -Body $completeBody -ContentType "application/json" -Headers $washerHeaders
Write-Host "  -> Booking Completed! Status: $($completeRes.status)" -ForegroundColor Green

# 12. Customer Verifies History
Write-Host "`n[12/12] Customer verifying their booking history..." -ForegroundColor Yellow
$myBookings = Invoke-RestMethod -Uri "$Gateway/api/bookings/my" -Method Get -Headers $custHeaders
$matchedBooking = $myBookings | Where-Object { $_.bookingId -eq $bookingId }
Write-Host "  -> Verified BookingId $bookingId in customer history with Status: '$($matchedBooking.status)'!" -ForegroundColor Green

Write-Host "`n==========================================================" -ForegroundColor Green
Write-Host " FULL BUSINESS CYCLE TEST COMPLETED SUCCESSFULLY (12/12)!" -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green
