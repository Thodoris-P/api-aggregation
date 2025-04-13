using ApiAggregation.Aggregation.Models;
using ApiAggregation.Aggregation.Services;
using ApiAggregation.ExternalApis.Abstractions;
using ApiAggregation.ExternalApis.Models;
using Bogus;
using Moq;
using Shouldly;

namespace ApiAggregation.UnitTests;


public class AggregatorServiceTests
{
    private readonly Faker _faker;
    private readonly CancellationToken _cancellationToken = CancellationToken.None;
    private readonly IExternalApiFilter _filterOptionsMock;

    public AggregatorServiceTests()
    {
        _faker = new Faker();
        var mockFilter = new Mock<IExternalApiFilter>();
        _filterOptionsMock = mockFilter.Object;
    }

    [Fact]
    public async Task GetAggregatedDataAsync_ReturnsAggregatedData_WhenApiClientsReturnData()
    {
        // Arrange
        // Create two fake responses with Bogus
        var fakeResponse1 = new ApiResponse
        {
            ApiName = _faker.Company.CompanyName(),
            Content = """{ "test": "test" }"""
        };
        var fakeResponse2 = new ApiResponse
        {
            ApiName = _faker.Company.CompanyName(),
            Content = """{ "test": "test" }"""
        };

        // Create mocks for external API clients using Moq
        var apiClientMock1 = new Mock<IExternalApiClient>();
        var apiClientMock2 = new Mock<IExternalApiClient>();

        apiClientMock1.Setup(client => client.GetDataAsync(_filterOptionsMock, _cancellationToken))
                      .ReturnsAsync(fakeResponse1);
        apiClientMock2.Setup(client => client.GetDataAsync(_filterOptionsMock, _cancellationToken))
                      .ReturnsAsync(fakeResponse2);

        var clients = new List<IExternalApiClient>
        {
            apiClientMock1.Object,
            apiClientMock2.Object
        };

        var aggregatorService = new AggregatorService(clients);

        // Act
        var result = await aggregatorService.GetAggregatedDataAsync(_filterOptionsMock);

        // Assert
        result.ShouldNotBeNull();
        result.ApiResponses.ShouldNotBeNull();
        result.ApiResponses.Count.ShouldBe(2);
        string actualResponse1 = result.ApiResponses[fakeResponse1.ApiName].ToString();
        actualResponse1.ShouldBe(fakeResponse1.Content);
        string actualResponse2 = result.ApiResponses[fakeResponse1.ApiName].ToString();
        actualResponse2.ShouldBe(fakeResponse2.Content);
    }

    [Fact]
    public async Task GetAggregatedDataAsync_ThrowsException_WhenAnyApiClientThrows()
    {
        // Arrange
        // Set up a mock API client to throw an exception
        var failingClientMock = new Mock<IExternalApiClient>();
        failingClientMock.Setup(client => client.GetDataAsync(_filterOptionsMock, _cancellationToken))
                         .ThrowsAsync(new Exception("API failure"));

        var clients = new List<IExternalApiClient>
        {
            failingClientMock.Object
        };

        var aggregatorService = new AggregatorService(clients);

        // Act & Assert: verify that the exception is propagated
        await Assert.ThrowsAsync<Exception>(() =>
            aggregatorService.GetAggregatedDataAsync(_filterOptionsMock));
    }

    [Fact]
    public async Task GetAggregatedDataAsync_ReturnsEmptyAggregatedData_WhenNoApiClients()
    {
        // Arrange
        // Use an empty list of API clients
        var aggregatorService = new AggregatorService(new List<IExternalApiClient>());

        // Act
        var result = await aggregatorService.GetAggregatedDataAsync(_filterOptionsMock);

        // Assert: result should have an empty dictionary
        Assert.NotNull(result);
        Assert.Empty(result.ApiResponses);
    }
}