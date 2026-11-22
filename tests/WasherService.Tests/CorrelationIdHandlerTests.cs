namespace WasherService.Tests;

using CarWash.Common.Http;
using CarWash.Common.Middleware;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

public class CorrelationIdHandlerTests
{
    [Fact]
    public async Task SendAsync_ShouldForwardCorrelationIdHeader_WhenPresentInHttpContext()
    {
        // Arrange
        var expectedCorrelationId = "test-correlation-id-789";
        var httpContext = new DefaultHttpContext();
        httpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey] = expectedCorrelationId;

        var mockAccessor = new Mock<IHttpContextAccessor>();
        mockAccessor.Setup(a => a.HttpContext).Returns(httpContext);

        var testHandler = new TestHttpMessageHandler();
        var correlationHandler = new CorrelationIdHandler(mockAccessor.Object)
        {
            InnerHandler = testHandler
        };

        using var client = new HttpClient(correlationHandler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/test");

        // Act
        await client.SendAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(testHandler.LastRequest);
        Assert.True(testHandler.LastRequest.Headers.Contains(CorrelationIdMiddleware.CorrelationIdHeaderName));
        var sentValue = testHandler.LastRequest.Headers.GetValues(CorrelationIdMiddleware.CorrelationIdHeaderName).FirstOrDefault();
        Assert.Equal(expectedCorrelationId, sentValue);
    }

    private sealed class TestHttpMessageHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }
}
