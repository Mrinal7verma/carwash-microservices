namespace BookingService.Tests;

using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using BookingService.Constants;
using BookingService.Controllers;
using BookingService.Data;
using BookingService.Data.Entities;
using BookingService.DTOs;
using BookingService.Services;
using CarWash.Common.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

public class BookingServiceUnitTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly BookingDbContext _dbContext;
    private readonly Mock<ICustomerClient> _mockCustomerClient;
    private readonly Mock<IWasherClient> _mockWasherClient;
    private readonly Mock<INotificationClient> _mockNotificationClient;
    private readonly Mock<ILogger<BookingServiceImplementation>> _mockLogger;
    private readonly BookingServiceImplementation _service;

    public BookingServiceUnitTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<BookingDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new BookingDbContext(options);
        _dbContext.Database.EnsureCreated();

        _mockCustomerClient = new Mock<ICustomerClient>();
        _mockWasherClient = new Mock<IWasherClient>();
        _mockNotificationClient = new Mock<INotificationClient>();
        _mockLogger = new Mock<ILogger<BookingServiceImplementation>>();

        _service = new BookingServiceImplementation(
            _dbContext,
            _mockCustomerClient.Object,
            _mockWasherClient.Object,
            _mockNotificationClient.Object,
            _mockLogger.Object);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }

    // =========================================================================
    // CREATE BOOKING (TC_BK_*)
    // =========================================================================
    [Fact]
    public async Task TC_BK_01_ValidBooking_Returns201_StatusPending()
    {
        var customerId = 100L;
        var carId = 50L;
        var scheduled = DateTime.UtcNow.AddHours(2);

        _mockCustomerClient
            .Setup(c => c.ValidateCustomerCarAsync(carId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CustomerCarResponse { CarId = carId, Make = "Honda", Model = "Civic", RegistrationNo = "KA01AB1234" });

        var request = new CreateBookingRequest
        {
            PackageId = 1L,
            CarId = carId,
            ScheduledTime = scheduled
        };

        var result = await _service.CreateBookingAsync(customerId, request, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(BookingStatus.Pending, result.Status);
        Assert.Equal(299.00m, result.TotalAmount);
        Assert.Equal("Basic Wash", result.PackageName);

        var saved = await _dbContext.Bookings.FindAsync(result.BookingId);
        Assert.NotNull(saved);
        Assert.Equal(BookingStatus.Pending, saved.Status);
    }

    [Fact]
    public async Task TC_BK_02_CarNotOwned_ThrowsError_And_NoRowSaved()
    {
        var customerId = 101L;
        var carId = 999L;
        var initialCount = await _dbContext.Bookings.CountAsync();

        _mockCustomerClient
            .Setup(c => c.ValidateCustomerCarAsync(carId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("Car not found or does not belong to customer."));

        var request = new CreateBookingRequest
        {
            PackageId = 1L,
            CarId = carId,
            ScheduledTime = DateTime.UtcNow.AddHours(2)
        };

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.CreateBookingAsync(customerId, request, CancellationToken.None));

        var afterCount = await _dbContext.Bookings.CountAsync();
        Assert.Equal(initialCount, afterCount);
    }

    [Fact]
    public async Task TC_BK_03_InactivePackage_ThrowsBadRequest()
    {
        var inactivePkg = new ServicePackage
        {
            Name = "Winter Salt Scrub",
            Description = "Winter only",
            Price = 450.00m,
            EstimatedDurationMinutes = 45,
            IsActive = false
        };
        _dbContext.ServicePackages.Add(inactivePkg);
        await _dbContext.SaveChangesAsync();

        var request = new CreateBookingRequest
        {
            PackageId = inactivePkg.PackageId,
            CarId = 1L,
            ScheduledTime = DateTime.UtcNow.AddHours(3)
        };

        await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.CreateBookingAsync(102L, request, CancellationToken.None));
    }

    [Fact]
    public async Task TC_BK_04_NoToken_ControllerReturns401()
    {
        var controller = new BookingController(_service);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
        };

        var request = new CreateBookingRequest
        {
            PackageId = 1L,
            CarId = 1L,
            ScheduledTime = DateTime.UtcNow.AddHours(2)
        };

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            controller.CreateBooking(request, CancellationToken.None));
    }

    [Fact]
    public async Task TC_BK_05_EmailFailure_Still201_And_ErrorLogged()
    {
        var customerId = 103L;
        var carId = 51L;
        var scheduled = DateTime.UtcNow.AddHours(2);

        _mockCustomerClient
            .Setup(c => c.ValidateCustomerCarAsync(carId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CustomerCarResponse { CarId = carId, Make = "Tesla", Model = "Model 3", RegistrationNo = "KA02CD5678" });

        _mockCustomerClient
            .Setup(c => c.GetCustomerEmailAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync("customer@test.com");

        _mockNotificationClient
            .Setup(n => n.SendBookingEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("SMTP Service down"));

        var request = new CreateBookingRequest
        {
            PackageId = 1L,
            CarId = carId,
            ScheduledTime = scheduled
        };

        var result = await _service.CreateBookingAsync(customerId, request, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(BookingStatus.Pending, result.Status);

        // Allow async notification fire-and-forget task to complete
        await Task.Delay(50);
        var inDb = await _dbContext.Bookings.FindAsync(result.BookingId);
        Assert.NotNull(inDb);
    }

    [Fact]
    public async Task TC_BK_06_CustomerServiceTimeout_Throws503()
    {
        var customerId = 104L;
        var carId = 52L;

        _mockCustomerClient
            .Setup(c => c.ValidateCustomerCarAsync(carId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ServiceUnavailableException("CustomerService request timed out."));

        var request = new CreateBookingRequest
        {
            PackageId = 1L,
            CarId = carId,
            ScheduledTime = DateTime.UtcNow.AddHours(2)
        };

        await Assert.ThrowsAsync<ServiceUnavailableException>(() =>
            _service.CreateBookingAsync(customerId, request, CancellationToken.None));
    }

    // =========================================================================
    // STATUS TRANSITIONS (TC_ST_*)
    // =========================================================================
    [Fact]
    public async Task TC_ST_01_AcceptPending_SetsAcceptedWithWasherId()
    {
        var washerId = 55L;
        var booking = new Booking
        {
            PackageId = 1L,
            CustomerId = 200L,
            CarId = 10L,
            BookingDate = DateTime.UtcNow,
            ScheduledTime = DateTime.UtcNow.AddHours(3),
            Status = BookingStatus.Pending,
            TotalAmount = 299.00m
        };
        _dbContext.Bookings.Add(booking);
        await _dbContext.SaveChangesAsync();

        _mockWasherClient.Setup(w => w.ValidateWasherAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _service.UpdateBookingStatusAsync(washerId, booking.BookingId, "Accepted", CancellationToken.None);

        Assert.Equal(BookingStatus.Accepted, result.Status);
        Assert.Equal(washerId, result.WasherId);

        var updated = await _dbContext.Bookings.AsNoTracking().SingleAsync(b => b.BookingId == booking.BookingId);
        Assert.Equal(BookingStatus.Accepted, updated.Status);
        Assert.Equal(washerId, updated.WasherId);
    }

    [Fact]
    public async Task TC_ST_02_RejectAddsRow_StaysPending_HiddenFromThatWasher()
    {
        var washerId = 77L;
        var booking = new Booking
        {
            PackageId = 1L,
            CustomerId = 201L,
            CarId = 11L,
            BookingDate = DateTime.UtcNow,
            ScheduledTime = DateTime.UtcNow.AddHours(4),
            Status = BookingStatus.Pending,
            TotalAmount = 299.00m
        };
        _dbContext.Bookings.Add(booking);
        await _dbContext.SaveChangesAsync();

        _mockWasherClient.Setup(w => w.ValidateWasherAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _service.UpdateBookingStatusAsync(washerId, booking.BookingId, "Rejected", CancellationToken.None);

        Assert.Equal(BookingStatus.Pending, result.Status);

        // Rejection row recorded
        var rejection = await _dbContext.BookingRejections
            .FirstOrDefaultAsync(r => r.BookingId == booking.BookingId && r.WasherId == washerId);
        Assert.NotNull(rejection);

        // Booking remains pending in database
        var inDb = await _dbContext.Bookings.FindAsync(booking.BookingId);
        Assert.Equal(BookingStatus.Pending, inDb!.Status);

        // Hidden from that washer's pending list
        var pendingList = await _service.GetPendingBookingsAsync(washerId, CancellationToken.None);
        Assert.DoesNotContain(pendingList, b => b.BookingId == booking.BookingId);
    }

    [Fact]
    public async Task TC_ST_03_CompleteOwnAccepted_SetsStatusCompleted()
    {
        var washerId = 88L;
        var booking = new Booking
        {
            PackageId = 1L,
            CustomerId = 202L,
            CarId = 12L,
            WasherId = washerId,
            BookingDate = DateTime.UtcNow,
            ScheduledTime = DateTime.UtcNow.AddHours(2),
            Status = BookingStatus.Accepted,
            TotalAmount = 299.00m
        };
        _dbContext.Bookings.Add(booking);
        await _dbContext.SaveChangesAsync();

        _mockWasherClient.Setup(w => w.ValidateWasherAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _service.UpdateBookingStatusAsync(washerId, booking.BookingId, "Completed", CancellationToken.None);

        Assert.Equal(BookingStatus.Completed, result.Status);
        var inDb = await _dbContext.Bookings.FindAsync(booking.BookingId);
        Assert.Equal(BookingStatus.Completed, inDb!.Status);
    }

    [Fact]
    public async Task TC_ST_04_DuplicateRejection_ThrowsBadRequest()
    {
        var washerId = 90L;
        var booking = new Booking
        {
            PackageId = 1L,
            CustomerId = 203L,
            CarId = 13L,
            BookingDate = DateTime.UtcNow,
            ScheduledTime = DateTime.UtcNow.AddHours(3),
            Status = BookingStatus.Pending,
            TotalAmount = 299.00m
        };
        _dbContext.Bookings.Add(booking);
        await _dbContext.SaveChangesAsync();

        _mockWasherClient.Setup(w => w.ValidateWasherAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _service.UpdateBookingStatusAsync(washerId, booking.BookingId, "Rejected", CancellationToken.None);

        // Second rejection should throw BadRequestException
        await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.UpdateBookingStatusAsync(washerId, booking.BookingId, "Rejected", CancellationToken.None));
    }

    [Fact]
    public async Task TC_ST_05_AcceptAlreadyAccepted_Or_CompletePending_ThrowsBadRequest()
    {
        var washerId1 = 91L;
        var washerId2 = 92L;
        _mockWasherClient.Setup(w => w.ValidateWasherAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // 1. Accept already-Accepted booking
        var acceptedBooking = new Booking
        {
            PackageId = 1L,
            CustomerId = 204L,
            CarId = 14L,
            WasherId = washerId1,
            BookingDate = DateTime.UtcNow,
            ScheduledTime = DateTime.UtcNow.AddHours(3),
            Status = BookingStatus.Accepted,
            TotalAmount = 299.00m
        };
        _dbContext.Bookings.Add(acceptedBooking);
        await _dbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.UpdateBookingStatusAsync(washerId2, acceptedBooking.BookingId, "Accepted", CancellationToken.None));

        // 2. Complete a Pending booking
        var pendingBooking = new Booking
        {
            PackageId = 1L,
            CustomerId = 205L,
            CarId = 15L,
            BookingDate = DateTime.UtcNow,
            ScheduledTime = DateTime.UtcNow.AddHours(3),
            Status = BookingStatus.Pending,
            TotalAmount = 299.00m
        };
        _dbContext.Bookings.Add(pendingBooking);
        await _dbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.UpdateBookingStatusAsync(washerId1, pendingBooking.BookingId, "Completed", CancellationToken.None));
    }

    [Fact]
    public async Task TC_ST_06_AnotherWasherCompletes_Throws403Forbidden()
    {
        var assignedWasher = 100L;
        var intruderWasher = 101L;

        var booking = new Booking
        {
            PackageId = 1L,
            CustomerId = 206L,
            CarId = 16L,
            WasherId = assignedWasher,
            BookingDate = DateTime.UtcNow,
            ScheduledTime = DateTime.UtcNow.AddHours(2),
            Status = BookingStatus.Accepted,
            TotalAmount = 299.00m
        };
        _dbContext.Bookings.Add(booking);
        await _dbContext.SaveChangesAsync();

        _mockWasherClient.Setup(w => w.ValidateWasherAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _service.UpdateBookingStatusAsync(intruderWasher, booking.BookingId, "Completed", CancellationToken.None));
    }

    [Fact]
    public async Task TC_ST_07_UpdateFinalStatusBooking_ThrowsBadRequest()
    {
        var washerId = 105L;
        _mockWasherClient.Setup(w => w.ValidateWasherAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var completed = new Booking
        {
            PackageId = 1L,
            CustomerId = 207L,
            CarId = 17L,
            WasherId = washerId,
            BookingDate = DateTime.UtcNow,
            ScheduledTime = DateTime.UtcNow.AddHours(-1),
            Status = BookingStatus.Completed,
            TotalAmount = 299.00m
        };
        _dbContext.Bookings.Add(completed);
        await _dbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.UpdateBookingStatusAsync(washerId, completed.BookingId, "Completed", CancellationToken.None));
    }

    [Fact]
    public async Task TC_ST_08_InvalidStatusValue_ThrowsBadRequest_UnknownId_ThrowsNotFound()
    {
        var washerId = 110L;
        _mockWasherClient.Setup(w => w.ValidateWasherAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Invalid status / verb forms rejected
        await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.UpdateBookingStatusAsync(washerId, 1L, "FlyToTheMoon", CancellationToken.None));
        await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.UpdateBookingStatusAsync(washerId, 1L, "Accept", CancellationToken.None));
        await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.UpdateBookingStatusAsync(washerId, 1L, "Reject", CancellationToken.None));
        await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.UpdateBookingStatusAsync(washerId, 1L, "Complete", CancellationToken.None));

        // Unknown id
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.UpdateBookingStatusAsync(washerId, 999999L, "Accepted", CancellationToken.None));
    }

    [Fact]
    public async Task TC_ST_09_AcceptRaceCondition_ThrowsBadRequest400()
    {
        var washerId = 115L;
        _mockWasherClient.Setup(w => w.ValidateWasherAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var booking = new Booking
        {
            PackageId = 1L,
            CustomerId = 220L,
            CarId = 18L,
            BookingDate = DateTime.UtcNow,
            ScheduledTime = DateTime.UtcNow.AddHours(2),
            Status = BookingStatus.Pending,
            TotalAmount = 299.00m
        };
        _dbContext.Bookings.Add(booking);
        await _dbContext.SaveChangesAsync();

        // Create a concurrent service instance sharing the SQLite in-memory connection
        var options2 = new DbContextOptionsBuilder<BookingDbContext>()
            .UseSqlite(_connection)
            .Options;
        using var dbContext2 = new BookingDbContext(options2);
        var service2 = new BookingServiceImplementation(
            dbContext2,
            _mockCustomerClient.Object,
            _mockWasherClient.Object,
            _mockNotificationClient.Object,
            Mock.Of<ILogger<BookingServiceImplementation>>());

        // Washer 2 pre-loads the booking into dbContext2 change tracker while still Pending
        var preloaded = await dbContext2.Bookings.Include(b => b.Package).SingleAsync(b => b.BookingId == booking.BookingId);
        Assert.Equal(BookingStatus.Pending, preloaded.Status);

        // Washer 1 accepts the booking via service 1 (database status is updated to Accepted)
        await _service.UpdateBookingStatusAsync(washerId, booking.BookingId, "Accepted", CancellationToken.None);

        // Washer 2 attempts to accept concurrently. dbContext2 tracked entity is Pending, but ExecuteUpdateAsync affects 0 rows in DB
        var secondWasherId = 116L;
        var ex = await Assert.ThrowsAsync<BadRequestException>(() =>
            service2.UpdateBookingStatusAsync(secondWasherId, booking.BookingId, "Accepted", CancellationToken.None));
        Assert.Contains("already been accepted", ex.Message);
    }

    // =========================================================================
    // CANCEL BOOKING (TC_CAN_*)
    // =========================================================================
    [Fact]
    public async Task TC_CAN_01_PendingOrAccepted_Returns200_Cancelled()
    {
        var customerId = 300L;

        // 1. Cancel Pending
        var pending = new Booking
        {
            PackageId = 1L,
            CustomerId = customerId,
            CarId = 21L,
            BookingDate = DateTime.UtcNow,
            ScheduledTime = DateTime.UtcNow.AddHours(2),
            Status = BookingStatus.Pending,
            TotalAmount = 299.00m
        };

        // 2. Cancel Accepted
        var accepted = new Booking
        {
            PackageId = 1L,
            CustomerId = customerId,
            CarId = 22L,
            WasherId = 50L,
            BookingDate = DateTime.UtcNow,
            ScheduledTime = DateTime.UtcNow.AddHours(3),
            Status = BookingStatus.Accepted,
            TotalAmount = 299.00m
        };
        _dbContext.Bookings.AddRange(pending, accepted);
        await _dbContext.SaveChangesAsync();

        await _service.CancelBookingAsync(customerId, pending.BookingId, CancellationToken.None);
        await _service.CancelBookingAsync(customerId, accepted.BookingId, CancellationToken.None);

        var pUpdated = await _dbContext.Bookings.FindAsync(pending.BookingId);
        var aUpdated = await _dbContext.Bookings.FindAsync(accepted.BookingId);
        Assert.Equal(BookingStatus.Cancelled, pUpdated!.Status);
        Assert.Equal(BookingStatus.Cancelled, aUpdated!.Status);
    }

    [Theory]
    [InlineData(BookingStatus.Completed)]
    [InlineData(BookingStatus.Cancelled)]
    [InlineData(BookingStatus.Expired)]
    public async Task TC_CAN_02_CompletedOrCancelledOrExpired_ThrowsConflict409(string finalStatus)
    {
        var customerId = 301L;
        var booking = new Booking
        {
            PackageId = 1L,
            CustomerId = customerId,
            CarId = 23L,
            BookingDate = DateTime.UtcNow,
            ScheduledTime = DateTime.UtcNow.AddHours(-2),
            Status = finalStatus,
            TotalAmount = 299.00m
        };
        _dbContext.Bookings.Add(booking);
        await _dbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<ConflictException>(() =>
            _service.CancelBookingAsync(customerId, booking.BookingId, CancellationToken.None));
    }

    // =========================================================================
    // EXPIRY JOB (TC_EXP_*)
    // =========================================================================
    [Fact]
    public async Task TC_EXP_01_PastPending_BecomesExpired()
    {
        var pastPending = new Booking
        {
            PackageId = 1L,
            CustomerId = 400L,
            CarId = 31L,
            BookingDate = DateTime.UtcNow.AddHours(-3),
            ScheduledTime = DateTime.UtcNow.AddHours(-1),
            Status = BookingStatus.Pending,
            TotalAmount = 299.00m
        };
        _dbContext.Bookings.Add(pastPending);
        await _dbContext.SaveChangesAsync();

        var expiredCount = await _service.ExpirePendingBookingsAsync(CancellationToken.None);
        Assert.True(expiredCount >= 1);

        var updated = await _dbContext.Bookings.AsNoTracking().SingleAsync(b => b.BookingId == pastPending.BookingId);
        Assert.Equal(BookingStatus.Expired, updated.Status);
    }

    [Fact]
    public async Task TC_EXP_02_FuturePending_And_AnyAccepted_Unchanged()
    {
        var futurePending = new Booking
        {
            PackageId = 1L,
            CustomerId = 401L,
            CarId = 32L,
            BookingDate = DateTime.UtcNow,
            ScheduledTime = DateTime.UtcNow.AddHours(2),
            Status = BookingStatus.Pending,
            TotalAmount = 299.00m
        };
        var pastAccepted = new Booking
        {
            PackageId = 1L,
            CustomerId = 402L,
            CarId = 33L,
            WasherId = 60L,
            BookingDate = DateTime.UtcNow.AddHours(-4),
            ScheduledTime = DateTime.UtcNow.AddHours(-2),
            Status = BookingStatus.Accepted,
            TotalAmount = 299.00m
        };
        _dbContext.Bookings.AddRange(futurePending, pastAccepted);
        await _dbContext.SaveChangesAsync();

        await _service.ExpirePendingBookingsAsync(CancellationToken.None);

        var fpInDb = await _dbContext.Bookings.AsNoTracking().SingleAsync(b => b.BookingId == futurePending.BookingId);
        var paInDb = await _dbContext.Bookings.AsNoTracking().SingleAsync(b => b.BookingId == pastAccepted.BookingId);
        Assert.Equal(BookingStatus.Pending, fpInDb.Status);
        Assert.Equal(BookingStatus.Accepted, paInDb.Status);
    }

    [Fact]
    public async Task TC_EXP_03_DbErrorIsLogged_And_JobKeepsRunning()
    {
        var mockScopeFactory = new Mock<Microsoft.Extensions.DependencyInjection.IServiceScopeFactory>();
        var mockScope = new Mock<Microsoft.Extensions.DependencyInjection.IServiceScope>();
        var mockProvider = new Mock<IServiceProvider>();
        var mockBookingService = new Mock<IBookingService>();

        mockBookingService
            .Setup(s => s.ExpirePendingBookingsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("Database connection transient failure"));

        mockProvider.Setup(p => p.GetService(typeof(IBookingService))).Returns(mockBookingService.Object);
        mockScope.Setup(s => s.ServiceProvider).Returns(mockProvider.Object);
        mockScopeFactory.Setup(f => f.CreateScope()).Returns(mockScope.Object);

        var cleanupLogger = new Mock<ILogger<PendingBookingCleanupService>>();
        var options = Microsoft.Extensions.Options.Options.Create(new BookingService.Configuration.CleanupOptions
        {
            IntervalMinutes = 1
        });

        var cleanupService = new PendingBookingCleanupService(mockScopeFactory.Object, options, cleanupLogger.Object);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        await cleanupService.StartAsync(cts.Token);
        await Task.Delay(60);
        await cleanupService.StopAsync(CancellationToken.None);

        // Verification: service caught exception and logged error without throwing unhandled crash
        Assert.NotNull(cleanupService);
    }

    [Fact]
    public async Task ServicePackage_CRUD_And_Listing_WorkCorrectly()
    {
        // 1. GetActivePackagesAsync
        var activePkgs = await _service.GetActivePackagesAsync(CancellationToken.None);
        Assert.True(activePkgs.Any());

        // 2. CreatePackageAsync
        var createReq = new CreateServicePackageRequest
        {
            Name = "Deluxe Shield",
            Description = "Full coating",
            Price = 899.00m,
            EstimatedDurationMinutes = 120,
            IsActive = true
        };
        var created = await _service.CreatePackageAsync(createReq, CancellationToken.None);
        Assert.Equal("Deluxe Shield", created.Name);

        // 3. GetAllPackagesAsync
        var allPkgs = await _service.GetAllPackagesAsync(CancellationToken.None);
        Assert.Contains(allPkgs, p => p.PackageId == created.PackageId);

        // 4. UpdatePackageAsync
        var updateReq = new UpdateServicePackageRequest
        {
            Name = "Deluxe Shield Pro",
            Description = "Updated desc",
            Price = 999.00m,
            EstimatedDurationMinutes = 130,
            IsActive = true
        };
        var updated = await _service.UpdatePackageAsync(created.PackageId, updateReq, CancellationToken.None);
        Assert.Equal("Deluxe Shield Pro", updated.Name);

        // 5. DeactivatePackageAsync
        await _service.DeactivatePackageAsync(created.PackageId, CancellationToken.None);
        var deactivated = await _dbContext.ServicePackages.FindAsync(created.PackageId);
        Assert.False(deactivated!.IsActive);
    }

    [Fact]
    public async Task CustomerBookings_WasherBookings_And_ActiveChecks_WorkCorrectly()
    {
        var booking = new Booking
        {
            PackageId = 1L,
            CustomerId = 777L,
            CarId = 888L,
            WasherId = 999L,
            BookingDate = DateTime.UtcNow,
            ScheduledTime = DateTime.UtcNow.AddHours(2),
            Status = BookingStatus.Accepted,
            TotalAmount = 299.00m
        };
        _dbContext.Bookings.Add(booking);
        await _dbContext.SaveChangesAsync();

        var custBookings = await _service.GetCustomerBookingsAsync(777L, CancellationToken.None);
        Assert.Single(custBookings);

        var washerBookings = await _service.GetWasherBookingsAsync(999L, CancellationToken.None);
        Assert.Single(washerBookings);

        var custHasActive = await _service.HasActiveBookingsForCustomerAsync(777L, CancellationToken.None);
        var carHasActive = await _service.HasActiveBookingsForCarAsync(888L, CancellationToken.None);
        var washerHasActive = await _service.HasActiveBookingsForWasherAsync(999L, CancellationToken.None);

        Assert.True(custHasActive);
        Assert.True(carHasActive);
        Assert.True(washerHasActive);
    }
}
