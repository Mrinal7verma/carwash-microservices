namespace CustomerService.Tests;

using CarWash.Common.Exceptions;
using Xunit;

public class ExceptionTests
{
    [Fact]
    public void CustomExceptions_ShouldHaveExpectedStatusCodes()
    {
        var notFound = new NotFoundException("Resource not found");
        var badRequest = new BadRequestException("Invalid input");
        var unauthorized = new UnauthorizedException("Not logged in");
        var forbidden = new ForbiddenException("Access denied");
        var conflict = new ConflictException("Already exists");
        var unavailable = new ServiceUnavailableException("Downstream down");

        Assert.Equal(404, notFound.StatusCode);
        Assert.Equal(400, badRequest.StatusCode);
        Assert.Equal(401, unauthorized.StatusCode);
        Assert.Equal(403, forbidden.StatusCode);
        Assert.Equal(409, conflict.StatusCode);
        Assert.Equal(503, unavailable.StatusCode);
    }
}
