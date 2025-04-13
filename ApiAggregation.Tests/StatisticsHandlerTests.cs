using System.Net;
using ApiAggregation.Statistics;
using Bogus;
using Moq;
using Moq.Protected;

namespace ApiAggregation.UnitTests;

public class StatisticsHandlerTests
{
    [Fact]
    public async Task SendAsync_WhenSuccessful_UpdatesStatisticsAndReturnsResponse()
    {
        // Arrange: create mocks locally.
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        var statisticsServiceMock = new Mock<IStatisticsService>(MockBehavior.Strict);

        var fakeHost = "example.com";
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK);

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(expectedResponse)
            .Verifiable();

        statisticsServiceMock
            .Setup(s => s.UpdateApiStatistics(fakeHost, It.Is<long>(ms => ms > 0)))
            .Verifiable();

        var statisticsHandler = new StatisticsHandler(statisticsServiceMock.Object)
        {
            InnerHandler = handlerMock.Object
        };

        var invoker = new HttpMessageInvoker(statisticsHandler, true);
        var request = new HttpRequestMessage(HttpMethod.Get, $"http://{fakeHost}/api/values");

        // Act
        var response = await invoker.SendAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(expectedResponse, response);
        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
        statisticsServiceMock.Verify(
            s => s.UpdateApiStatistics(fakeHost, It.Is<long>(ms => ms > 0)),
            Times.Once());
        // Ensure no extra calls.
        handlerMock.VerifyNoOtherCalls();
        statisticsServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SendAsync_WhenExceptionThrown_UpdatesStatisticsAndRethrowsException()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        var statisticsServiceMock = new Mock<IStatisticsService>(MockBehavior.Strict);
        var expectedException = new InvalidOperationException("Test exception");
        var fakeHost = "example.com";
    
        // Adjust the setup condition to allow 0 or more milliseconds.
        statisticsServiceMock
            .Setup(s => s.UpdateApiStatistics(fakeHost, It.Is<long>(ms => ms >= 0)))
            .Verifiable();

        // Setup the handler to throw an exception.
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(expectedException)
            .Verifiable();

        var statisticsHandler = new StatisticsHandler(statisticsServiceMock.Object)
        {
            InnerHandler = handlerMock.Object
        };

        var invoker = new HttpMessageInvoker(statisticsHandler, true);
        var request = new HttpRequestMessage(HttpMethod.Get, $"http://{fakeHost}/api/values");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            invoker.SendAsync(request, CancellationToken.None));

        Assert.Equal(expectedException.Message, exception.Message);
        statisticsServiceMock.Verify(s =>
                s.UpdateApiStatistics(fakeHost, It.Is<long>(ms => ms >= 0)),
            Times.Once);
        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }
}