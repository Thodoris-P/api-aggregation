using ApiAggregation.Aggregation;
using ApiAggregation.ExternalApis;
using Bogus;
using Moq;

namespace ApiAggregation.UnitTests;


public class AggregatorServiceTests
{
    private readonly Faker _faker;
    private readonly CancellationToken _cancellationToken = CancellationToken.None;


    public AggregatorServiceTests()
    {
        _faker = new Faker();
    }

    [Fact]
    public async Task GetAggregatedDataAsync_ReturnsAggregatedData_WhenApiClientsReturnData()
    {
        // Arrange
        var filterOptionsMock = new Mock<IExternalApiFilter>();

        // Create two fake responses with Bogus
        var response1 = new ApiResponse
        {
            ApiName = _faker.Company.CompanyName(),
            Content = _faker.Lorem.Sentence()
        };
        var response2 = new ApiResponse
        {
            ApiName = _faker.Company.CompanyName(),
            Content = _faker.Lorem.Sentence()
        };

        // Create mocks for external API clients using Moq
        var apiClientMock1 = new Mock<IExternalApiClient>();
        var apiClientMock2 = new Mock<IExternalApiClient>();

        apiClientMock1.Setup(client => client.GetDataAsync(filterOptionsMock.Object, _cancellationToken))
                      .ReturnsAsync(response1);
        apiClientMock2.Setup(client => client.GetDataAsync(filterOptionsMock.Object, _cancellationToken))
                      .ReturnsAsync(response2);

        var clients = new List<IExternalApiClient>
        {
            apiClientMock1.Object,
            apiClientMock2.Object
        };

        var aggregatorService = new AggregatorService(clients);

        // Act
        var result = await aggregatorService.GetAggregatedDataAsync(filterOptionsMock.Object);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.ApiResponses);
        Assert.Equal(2, result.ApiResponses.Count);
        Assert.Equal(response1.Content, result.ApiResponses[response1.ApiName]);
        Assert.Equal(response2.Content, result.ApiResponses[response2.ApiName]);
    }

    [Fact]
    public async Task GetAggregatedDataAsync_ThrowsException_WhenAnyApiClientThrows()
    {
        // Arrange
        var filterOptionsMock = new Mock<IExternalApiFilter>();

        // Set up a mock API client to throw an exception
        var failingClientMock = new Mock<IExternalApiClient>();
        failingClientMock.Setup(client => client.GetDataAsync(filterOptionsMock.Object, _cancellationToken))
                         .ThrowsAsync(new Exception("API failure"));

        var clients = new List<IExternalApiClient>
        {
            failingClientMock.Object
        };

        var aggregatorService = new AggregatorService(clients);

        // Act & Assert: verify that the exception is propagated
        await Assert.ThrowsAsync<Exception>(() =>
            aggregatorService.GetAggregatedDataAsync(filterOptionsMock.Object));
    }

    [Fact]
    public async Task GetAggregatedDataAsync_ReturnsEmptyAggregatedData_WhenNoApiClients()
    {
        // Arrange
        var filterOptionsMock = new Mock<IExternalApiFilter>();

        // Use an empty list of API clients
        var clients = new List<IExternalApiClient>();

        var aggregatorService = new AggregatorService(clients);

        // Act
        AggregatedData result = await aggregatorService.GetAggregatedDataAsync(filterOptionsMock.Object);

        // Assert: result should have an empty dictionary
        Assert.NotNull(result);
        Assert.Empty(result.ApiResponses);
    }
}