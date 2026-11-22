namespace CustomerService.Services;

using CarWash.Common.Exceptions;
using global::CustomerService.Data;
using global::CustomerService.Data.Entities;
using global::CustomerService.DTOs;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Business logic service handling customers, vehicles, and booking validation dependencies.
/// </summary>
public class CustomerService : ICustomerService
{
    private readonly CustomerDbContext _dbContext;
    private readonly IBookingClient _bookingClient;

    public CustomerService(CustomerDbContext dbContext, IBookingClient bookingClient)
    {
        _dbContext = dbContext;
        _bookingClient = bookingClient;
    }

    public async Task RegisterCustomerAsync(CustomerRegisterRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName) ||
            string.IsNullOrWhiteSpace(request.LastName) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            !request.Email.Contains('@') ||
            string.IsNullOrWhiteSpace(request.Phone) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            throw new BadRequestException("All required fields must be provided with valid values.");
        }

        var emailLower = request.Email.Trim().ToLowerInvariant();
        var exists = await _dbContext.Customers.AnyAsync(c => c.Email == emailLower, cancellationToken);
        if (exists)
        {
            throw new BadRequestException("A customer with this email address is already registered.");
        }

        var customer = new Customer
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = emailLower,
            Phone = request.Phone.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Address = request.Address?.Trim(),
            City = request.City?.Trim()
        };

        _dbContext.Customers.Add(customer);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Customer> ValidateCustomerAsync(string email, string password, CancellationToken cancellationToken)
    {
        var emailLower = email.Trim().ToLowerInvariant();
        var customer = await _dbContext.Customers
            .SingleOrDefaultAsync(c => c.Email == emailLower, cancellationToken);

        if (customer == null || !BCrypt.Net.BCrypt.Verify(password, customer.PasswordHash))
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        return customer;
    }

    public async Task<CustomerProfileResponse> GetProfileAsync(long customerId, CancellationToken cancellationToken)
    {
        var customer = await _dbContext.Customers
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.CustomerId == customerId, cancellationToken);

        if (customer == null)
        {
            throw new NotFoundException("Customer profile not found.");
        }

        return new CustomerProfileResponse
        {
            CustomerId = customer.CustomerId,
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            Email = customer.Email,
            Phone = customer.Phone,
            Address = customer.Address,
            City = customer.City
        };
    }

    public async Task<IEnumerable<CustomerProfileResponse>> GetAllCustomersAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Customers
            .AsNoTracking()
            .Select(c => new CustomerProfileResponse
            {
                CustomerId = c.CustomerId,
                FirstName = c.FirstName,
                LastName = c.LastName,
                Email = c.Email,
                Phone = c.Phone,
                Address = c.Address,
                City = c.City
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<string> GetCustomerEmailAsync(long customerId, CancellationToken cancellationToken)
    {
        var customer = await _dbContext.Customers
            .AsNoTracking()
            .Where(c => c.CustomerId == customerId)
            .Select(c => c.Email)
            .SingleOrDefaultAsync(cancellationToken);

        if (customer == null)
        {
            throw new NotFoundException("Customer not found.");
        }

        return customer;
    }

    public async Task<bool> CanDeleteCustomerAsync(long customerId, CancellationToken cancellationToken)
    {
        var hasActive = await _bookingClient.HasActiveCustomerBookingsAsync(customerId, cancellationToken);
        if (hasActive)
        {
            throw new ConflictException("Cannot delete customer account with active bookings.");
        }

        return true;
    }

    public async Task DeleteCustomerAsync(long customerId, CancellationToken cancellationToken)
    {
        var customer = await _dbContext.Customers
            .Include(c => c.Cars)
            .SingleOrDefaultAsync(c => c.CustomerId == customerId, cancellationToken);

        if (customer == null)
        {
            throw new NotFoundException("Customer profile not found.");
        }

        // Active booking check across BookingService
        await CanDeleteCustomerAsync(customerId, cancellationToken);

        _dbContext.Customers.Remove(customer);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<CarResponse> AddCarAsync(long customerId, CarRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Make) ||
            string.IsNullOrWhiteSpace(request.Model) ||
            string.IsNullOrWhiteSpace(request.RegistrationNo) ||
            request.RegistrationNo.Trim().Length > 20)
        {
            throw new BadRequestException("Invalid car details or registration number exceeds 20 characters.");
        }

        var customerExists = await _dbContext.Customers.AnyAsync(c => c.CustomerId == customerId, cancellationToken);
        if (!customerExists)
        {
            throw new NotFoundException("Customer not found.");
        }

        var regNormalized = request.RegistrationNo.Trim().ToUpperInvariant();
        var regExists = await _dbContext.Cars.AnyAsync(c => c.RegistrationNo == regNormalized, cancellationToken);
        if (regExists)
        {
            throw new BadRequestException("A vehicle with this registration number already exists.");
        }

        var car = new Car
        {
            CustomerId = customerId,
            Make = request.Make.Trim(),
            Model = request.Model.Trim(),
            RegistrationNo = regNormalized
        };

        _dbContext.Cars.Add(car);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CarResponse
        {
            CarId = car.CarId,
            Make = car.Make,
            Model = car.Model,
            RegistrationNo = car.RegistrationNo
        };
    }

    public async Task<IEnumerable<CarResponse>> GetCarsAsync(long customerId, CancellationToken cancellationToken)
    {
        return await _dbContext.Cars
            .AsNoTracking()
            .Where(c => c.CustomerId == customerId)
            .Select(c => new CarResponse
            {
                CarId = c.CarId,
                Make = c.Make,
                Model = c.Model,
                RegistrationNo = c.RegistrationNo
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<CarResponse> GetCarByIdAsync(long customerId, long carId, CancellationToken cancellationToken)
    {
        var car = await _dbContext.Cars
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.CarId == carId && c.CustomerId == customerId, cancellationToken);

        if (car == null)
        {
            throw new NotFoundException("Car not found or does not belong to the caller.");
        }

        return new CarResponse
        {
            CarId = car.CarId,
            Make = car.Make,
            Model = car.Model,
            RegistrationNo = car.RegistrationNo
        };
    }

    public async Task<bool> CanDeleteCarAsync(long carId, CancellationToken cancellationToken)
    {
        var hasActive = await _bookingClient.HasActiveCarBookingsAsync(carId, cancellationToken);
        if (hasActive)
        {
            throw new ConflictException("Cannot delete vehicle with active bookings.");
        }

        return true;
    }

    public async Task DeleteCarAsync(long customerId, long carId, CancellationToken cancellationToken)
    {
        var car = await _dbContext.Cars
            .SingleOrDefaultAsync(c => c.CarId == carId && c.CustomerId == customerId, cancellationToken);

        if (car == null)
        {
            throw new NotFoundException("Car not found or does not belong to the caller.");
        }

        // Active booking check across BookingService
        await CanDeleteCarAsync(carId, cancellationToken);

        _dbContext.Cars.Remove(car);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
