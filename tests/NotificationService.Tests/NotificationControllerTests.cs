namespace NotificationService.Tests;

using Microsoft.AspNetCore.Mvc;
using Moq;
using NotificationService.Controllers;
using NotificationService.DTOs;
using NotificationService.Services;
using Xunit;

public class NotificationControllerTests
{
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly NotificationController _controller;

    public NotificationControllerTests()
    {
        _mockEmailService = new Mock<IEmailService>();
        _controller = new NotificationController(_mockEmailService.Object);
    }

    [Fact]
    public async Task SendEmail_ShouldCallEmailServiceAndReturn200()
    {
        // Arrange
        var request = new SendEmailRequest
        {
            To = "customer@example.com",
            Subject = "Booking Confirmation",
            Body = "Your car wash is booked!"
        };

        // Act
        var result = await _controller.SendEmail(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<SendEmailResponse>(okResult.Value);
        Assert.Equal("Email processed successfully.", response.Message);

        _mockEmailService.Verify(s => s.SendAsync(request.To, request.Subject, request.Body, It.IsAny<CancellationToken>()), Times.Once);
    }
}
