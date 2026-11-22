namespace CustomerService.Controllers;

using CarWash.Common.Extensions;
using CustomerService.DTOs;
using CustomerService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Controller for managing customer vehicle registrations.
/// </summary>
[ApiController]
[Route("api/customers/cars")]
[Authorize(Roles = "Customer")]
public class CarController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CarController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CarResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AddCar([FromBody] CarRequest request, CancellationToken cancellationToken)
    {
        var customerId = User.GetUserId();
        var car = await _customerService.AddCarAsync(customerId, request, cancellationToken);
        return Ok(car);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CarResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCars(CancellationToken cancellationToken)
    {
        var customerId = User.GetUserId();
        var cars = await _customerService.GetCarsAsync(customerId, cancellationToken);
        return Ok(cars);
    }

    [HttpGet("{carId:long}")]
    [ProducesResponseType(typeof(CarResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCar([FromRoute] long carId, CancellationToken cancellationToken)
    {
        var customerId = User.GetUserId();
        var car = await _customerService.GetCarByIdAsync(customerId, carId, cancellationToken);
        return Ok(car);
    }

    [HttpDelete("{carId:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> DeleteCar([FromRoute] long carId, CancellationToken cancellationToken)
    {
        var customerId = User.GetUserId();
        await _customerService.DeleteCarAsync(customerId, carId, cancellationToken);
        return Ok(new { message = "Car deleted successfully" });
    }
}
