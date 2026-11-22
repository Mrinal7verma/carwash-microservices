namespace BookingService.Services;

using System.Net.Http.Headers;
using System.Net.Http.Json;
using BookingService.Configuration;
using BookingService.DTOs;
using CarWash.Common.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Typed client for CustomerService operations.
/// </summary>
public class CustomerClient : ICustomerClient
{
    private const string InternalApiKeyHeaderName = "X-Internal-Api-Key";

    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly InternalApiOptions _internalApiOptions;
    private readonly ILogger<CustomerClient> _logger;

    public CustomerClient(
        HttpClient httpClient,
        IHttpContextAccessor httpContextAccessor,
        IOptions<InternalApiOptions> internalApiOptions,
        ILogger<CustomerClient> logger)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
        _internalApiOptions = internalApiOptions.Value;
        _logger = logger;
    }

    public async Task<CustomerCarResponse> ValidateCustomerCarAsync(long carId, CancellationToken cancellationToken)
    {
        var relativeUri = $"api/customers/cars/{carId}";
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, relativeUri);

            // Forward the caller's Bearer token
            var authHeader = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
            if (!string.IsNullOrWhiteSpace(authHeader))
            {
                request.Headers.Add("Authorization", authHeader);
            }

            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                throw new NotFoundException("Car not found or does not belong to the customer.");
            }

            if ((int)response.StatusCode >= 500)
            {
                _logger.LogWarning("CustomerService responded with server error {StatusCode} for {Uri}", response.StatusCode, relativeUri);
                throw new ServiceUnavailableException("CustomerService returned a server error.");
            }

            response.EnsureSuccessStatusCode();

            var car = await response.Content.ReadFromJsonAsync<CustomerCarResponse>(cancellationToken: cancellationToken);
            if (car == null)
            {
                throw new NotFoundException("Car details could not be parsed.");
            }

            return car;
        }
        catch (NotFoundException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP connection error while validating car at CustomerService: {Uri}", relativeUri);
            throw new ServiceUnavailableException("CustomerService is currently unavailable.");
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Timeout occurred while validating car at CustomerService: {Uri}", relativeUri);
            throw new ServiceUnavailableException("CustomerService request timed out.");
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Timeout occurred while validating car at CustomerService: {Uri}", relativeUri);
            throw new ServiceUnavailableException("CustomerService request timed out.");
        }
    }

    public async Task<string> GetCustomerEmailAsync(long customerId, CancellationToken cancellationToken)
    {
        var relativeUri = $"api/customers/internal/{customerId}/email";
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, relativeUri);
            if (!string.IsNullOrWhiteSpace(_internalApiOptions.Key))
            {
                request.Headers.Add(InternalApiKeyHeaderName, _internalApiOptions.Key);
            }

            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                throw new NotFoundException("Customer not found.");
            }

            if ((int)response.StatusCode >= 500)
            {
                _logger.LogWarning("CustomerService internal lookup failed with status {StatusCode}", response.StatusCode);
                throw new ServiceUnavailableException("CustomerService internal lookup error.");
            }

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<CustomerEmailResponse>(cancellationToken: cancellationToken);
            if (result == null || string.IsNullOrWhiteSpace(result.Email))
            {
                throw new NotFoundException("Customer email not found.");
            }

            return result.Email;
        }
        catch (NotFoundException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP connection error while retrieving customer email from CustomerService: {Uri}", relativeUri);
            throw new ServiceUnavailableException("CustomerService is currently unavailable.");
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Timeout occurred while retrieving customer email from CustomerService: {Uri}", relativeUri);
            throw new ServiceUnavailableException("CustomerService request timed out.");
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Timeout occurred while retrieving customer email from CustomerService: {Uri}", relativeUri);
            throw new ServiceUnavailableException("CustomerService request timed out.");
        }
    }
}
