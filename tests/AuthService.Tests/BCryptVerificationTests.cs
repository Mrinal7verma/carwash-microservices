namespace AuthService.Tests;

using Xunit;

public class BCryptVerificationTests
{
    [Fact]
    public void VerifyAdminPasswordHash()
    {
        var hash = "$2b$11$qK4XTDyLDDdbEPZRd673IOY8ey/2JbOr9Yw.X.iefXbJmIjEADnqy";
        var password = "Admin@123";
        var matches = BCrypt.Net.BCrypt.Verify(password, hash);
        Assert.True(matches, "Hash provided in prompt did not match Admin@123");
    }
}
