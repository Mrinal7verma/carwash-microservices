namespace CustomerService.Controllers;

using CarWash.Common.Extensions;
using CarWash.Common.Security;
using CustomerService.DTOs;
using CustomerService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Controller handling customer account management, authentication, and internal lookups.
/// </summary>
[ApiController]
[Route("api/customers")]
public class CustomerController : ControllerBase
{
    private readonly ICustomerService _customerService;
    private readonly IJwtService _jwtService;

    public CustomerController(ICustomerService customerService, IJwtService jwtService)
    {
        _customerService = customerService;
        _jwtService = jwtService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] CustomerRegisterRequest request, CancellationToken cancellationToken)
    {
        await _customerService.RegisterCustomerAsync(request, cancellationToken);
        return Ok(new { message = "Customer registered successfully" });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CustomerLoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] CustomerLoginRequest request, CancellationToken cancellationToken)
    {
        var customer = await _customerService.ValidateCustomerAsync(request.Email, request.Password, cancellationToken);
        var token = _jwtService.GenerateToken(customer.CustomerId, customer.Email);
        return Ok(new CustomerLoginResponse { Token = token });
    }

    [HttpGet("profile")]
    [Authorize(Roles = "Customer")]
    [ProducesResponseType(typeof(CustomerProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var customerId = User.GetUserId();
        var profile = await _customerService.GetProfileAsync(customerId, cancellationToken);
        return Ok(profile);
    }

    [HttpDelete("profile")]
    [Authorize(Roles = "Customer")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> DeleteProfile(CancellationToken cancellationToken)
    {
        var customerId = User.GetUserId();
        await _customerService.DeleteCustomerAsync(customerId, cancellationToken);
        return Ok(new { message = "Customer profile deleted successfully" });
    }

    [HttpGet("admin/all")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(IEnumerable<CustomerProfileResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllCustomers(CancellationToken cancellationToken)
    {
        var customers = await _customerService.GetAllCustomersAsync(cancellationToken);
        return Ok(customers);
    }

    [HttpGet("internal/{customerId}/email")]
    [InternalApiKey]
    [ProducesResponseType(typeof(CustomerEmailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCustomerEmail([FromRoute] long customerId, CancellationToken cancellationToken)
    {
        var email = await _customerService.GetCustomerEmailAsync(customerId, cancellationToken);
        return Ok(new CustomerEmailResponse { Email = email });
    }
}
