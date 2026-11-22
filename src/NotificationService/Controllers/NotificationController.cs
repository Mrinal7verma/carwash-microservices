namespace NotificationService.Controllers;

using Microsoft.AspNetCore.Mvc;
using NotificationService.DTOs;
using NotificationService.Services;

/// <summary>
/// Internal controller for email notification endpoints.
/// </summary>
[ApiController]
[Route("api/notifications")]
public class NotificationController : ControllerBase
{
    private readonly IEmailService _emailService;

    public NotificationController(IEmailService emailService)
    {
        _emailService = emailService;
    }

    [HttpPost("email")]
    [ProducesResponseType(typeof(SendEmailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SendEmail([FromBody] SendEmailRequest request, CancellationToken cancellationToken)
    {
        await _emailService.SendAsync(request.To, request.Subject, request.Body, cancellationToken);
        return Ok(new SendEmailResponse { Message = "Email processed successfully." });
    }
}
