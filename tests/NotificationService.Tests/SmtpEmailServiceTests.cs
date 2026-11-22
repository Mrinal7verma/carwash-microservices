namespace NotificationService.Tests;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NotificationService.Configuration;
using NotificationService.Services;
using Xunit;

public class SmtpEmailServiceTests
{
    [Fact]
    public async Task SendAsync_ShouldSkipSendingAndReturn_WhenSmtpIsDisabled()
    {
        // Arrange
        var options = Options.Create(new SmtpOptions
        {
            Host = "non-existent-host",
            Port = 25,
            Enabled = false
        });

        var mockLogger = new Mock<ILogger<SmtpEmailService>>();
        var service = new SmtpEmailService(options, mockLogger.Object);

        // Act & Assert (Should not attempt connection or throw any socket/network exception)
        await service.SendAsync("test@example.com", "Test Subject", "Test Body", CancellationToken.None);
    }

    [Fact]
    public async Task SendAsync_ShouldAttemptConnection_WhenSmtpIsEnabled()
    {
        var options = Options.Create(new SmtpOptions
        {
            Host = "127.0.0.1",
            Port = 587,
            Enabled = true,
            FromAddress = "carwash@test.com",
            SenderName = "CarWash Alerts",
            Username = "testuser",
            Password = "testpassword"
        });

        var mockLogger = new Mock<ILogger<SmtpEmailService>>();
        var service = new SmtpEmailService(options, mockLogger.Object);

        await Assert.ThrowsAnyAsync<Exception>(() =>
            service.SendAsync("recipient@test.com", "Test Subject", "Test Body", CancellationToken.None));
    }
}
