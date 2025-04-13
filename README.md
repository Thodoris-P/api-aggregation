# API Aggregation Assignment Service

## Overview
This service is designed to aggregate data from multiple APIs and provide a unified response.
I could not come up with a clever idea of combining the entirely different APIs into one single response.
For that reason, I decided to create a simple API that will return the data from the APIs in a single response, and provide some filtering.

## Flexible API Integration Design
In order to allow for multiple and easy integration of third party APIs, I decided to use the adapter pattern.
The adapter pattern allows us to create a common interface for different APIs, making it easier to integrate and use them interchangeably.
It acts as a translator between our unified endpoint and interface and each individual external API.

### How it works
- Each external API integration implements a common BaseApiClient interface. This abstraction hides the complexities of JSON parsing, fallback mechanisms, logging, and exception handling behind a standardized contract.
- Adding a new API is as simple as creating a new adapter that adheres to the BaseApiClient contract. This minimizes the amount of refactoring needed when introducing new data sources.
- The adapter decouples the service logic from the external API specifics. Changes in the external APIs (like endpoint changes or additional authentication requirements) only necessitate updates to the corresponding adapter rather than the entire codebase.
  - In cases where the API needs something extra like authentication or special headers, it can easily be added to its adapter without affecting the rest of the code.

### Why
Maintainability: Changes or enhancements to one API integration don't directly impact others.

Scalability: New APIs can be added quickly without reworking the entire aggregation logic.

Testability: With a standard interface, each adapter can be individually mocked or tested, improving overall test coverage.

### Implementation specifics
All you need to do to add a new API is to create an Adapter implementing the BaseApiClient (that will handle json parsing, fallbacks, exceptions etc.)
You need to also inject the API settings (clientKey etc.) and adapt the request filter to your API needs.

### Caching
Utilizing the decorator pattern, we can easily add additional functionality --in our case caching-- to the API client,
without modifying the underlying API client code. It allows extending the behaviour of the api client without modifying its structure.


Current Implemented APIs:
- OpenWeatherMap API: https://openweathermap.org/api
- News API: https://newsapi.org/
- Spotify Web API

## API Endpoints
### POST /api/aggregation
I was thinking of a GET method to follow the RESTful architecture (since we get data with filters it should be GET),
but since we need to provide enough filters to integrate with all sorts of APIs, I decided to go with POST.
- **Description**: Aggregates data from multiple APIs based on the provided filters.
- **Request Body Example**: 
```json
{
  "keyword": "us",
  "city": "London",
  "country": "uk"
}
```
- **Response**: 
```json
{
    "apiResponses": {
        "OpenWeatherMap": {  },
        "WeatherAPI": {  },
        "Spotify": {  }
    }
}
```

### GET /api/statistics
- **Description**: Provides the statistics of the API performance grouped in performance buckets.
- **Response Example**:
```json
{
  "Fast": {},
  "Medium": {},
  "Slow": {
    "newsapi.org": {
      "averageResponseTime": 338,
      "minResponseTime": 338,
      "maxResponseTime": 338,
      "totalRequests": 1
    },
    "MyApiAggregator": {
      "averageResponseTime": 1270,
      "minResponseTime": 1270,
      "maxResponseTime": 1270,
      "totalRequests": 1
    }
  }
}
```

### POST /api/authentication/register
- **Description**: Registers a new user.
- **Request Body Example**:
```json
{
  "username": "exampleUser",
  "password": "StrongPassword123"
}
```
- **Response**:
```json
{
    "apiResponses": {
        "OpenWeatherMap": {  },
        "WeatherAPI": {  },
        "Spotify": {  }
    }
}
```


### POST /api/authentication/login
- **Description**: Logins a user and returns the jwt token and refreshToken.
- **Request Body Example**:
```json
{
  "username": "exampleUser",
  "password": "StrongPassword123"
}
```
- **Response**:
```json
{
  "isSuccessful": true,
  "message": "Login successful",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiI2ODVkYThiNi05NDVmLTQ2ZDctYjYyNi04NTNiZDBiMzg0MWIiLCJ1bmlxdWVfbmFtZSI6InRlbyIsIm5iZiI6MTc0NDU2NDc1MSwiZXhwIjoxNzQ0NTY1NjUxLCJpYXQiOjE3NDQ1NjQ3NTEsImlzcyI6Ik15QXBpIiwiYXVkIjoiTXlBcGlVc2VycyJ9.IzcE05tKvOiqS7mFJ3_N3v5rMjrfW6A3G2eqtAP4nHI",
  "refreshToken": "Gqv2UukQlJi4zLCuqE4BnHCTpyKOWkX6zXm4VNo0wq4="
}
```

### POST /api/authentication/refresh
- **Description**: Refreshes a user tokens.
- **Request Body Example**:
```json
{
  "refreshToken": "xhaHbryinPtiz1epkaocOj5XnAlQprKFHeLUHW6La+o="
}
```
- **Response**:
```json
{
  "isSuccessful": true,
  "message": "Token refreshed successfully",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiI2ODVkYThiNi05NDVmLTQ2ZDctYjYyNi04NTNiZDBiMzg0MWIiLCJ1bmlxdWVfbmFtZSI6InRlbyIsIm5iZiI6MTc0NDU2NDc1MSwiZXhwIjoxNzQ0NTY1NjUxLCJpYXQiOjE3NDQ1NjQ3NTEsImlzcyI6Ik15QXBpIiwiYXVkIjoiTXlBcGlVc2VycyJ9.IzcE05tKvOiqS7mFJ3_N3v5rMjrfW6A3G2eqtAP4nHI",
  "refreshToken": "Gqv2UukQlJi4zLCuqE4BnHCTpyKOWkX6zXm4VNo0wq4="
}
```


## HOW TO RUN
1. Clone the repository.
2. Open the solution in Visual Studio or your preferred IDE.
3. Restore the NuGet packages.
4. Make sure to API keys and JWT secret in the appsettings.json file.
   - I will provide those via email. In a real world scenario, you would use a secret manager (vaults or secure stores) or environment variables (Azure) to store sensitive information.
5. Run the application 

   5.1. using `dotnet run` or through your IDE.

   5.2. using `docker compose up` to spin up a container using the provided Dockerfile and Compose.
6. Use the .http file or any API testing tool to test the endpoints.


## Implementation Specifics
- Used Microsoft.Resilience for implementing circuit breaker, retry policies, fallback mechanisms (for external api failures)
- Wrote xUnit tests utilizing libraries like Moq, Bogus, Shouldly
- Used Microsoft.Extensions.Caching.Hybrid for caching. Current implementation is in-memory, but can be easily switched to Redis or any other distributed cache.
  - I went with HybridCache because it allows for both in-memory and distributed caching, which is useful for testing and production environments.
  - And also handles well concurrent requests which other in-memory caches do not.
- Use Task.WhenAll to make concurrent requests to all the APIs.
- Created an endpoint to get the Statistics of all the APIs (group them in performance buckets).
  - Use DelegatingHandler (StatisticsHandler) on the HttpClients to measure the time taken for each request.
  - Use a custom middleware (RequestStatisticsMiddleware) to measure the time taken for the entire request.
  - Use the StatisticsService to record the metrics and provide the statistic data in the apropriate format.
    - It also leverages HybridCache to cache the formated stats data, as to not indtroduce any problems with concurrent requests.
    - On a second thought I should have further decoupled some logic from the StatisticsService, like the formatting and the caching.
  - Implement a background service (PerformanceMonitoringService) to monitor Api performance and log alerts (On second thought, it should be injected with an alert provider, to send alerts to the service needed).
  - Implement a background service to clean up the gathered statistics (StatisticsCleanupService).
    - That was done because in this scope, the statistics are stored in memory, and we need to clean them up after a while.
    - I wanted to use a sliding window or circular buffer, but the bcl does not provide that functionality, so I went with the concurrent Queue.
- Use a JWT bearer authentication scheme, with an AccountsController to handle user registration and login by using a custom in-memory AccountService.
- Use Serilog for logging, and logging the requests

## Known Bugs:
- Statistics endpoint returns cached Empty Response.


## Final Notes & Observations & Feature Improvements:
- I tried to tackle each and every requirement in the task description, and I think that finally backfired.
- I should have put way more thought in the core requirement of aggregating the data from the APIs. Instead, I chose to implement the core architecture after the initial design and handle the filtering later (never happened).
- Given more time I would have made the code more testable and added more unit tests (test coverage is low).
  - For example, some classes have more than one responsibility, some classes use the stopwotch directly, etc.
- Apply better, more robust error handling and catch all possible exceptions.
- I would have also logged more consistently.
- I chose Microsoft HybridCache for caching, but it was a bit difficult to set up in tests and I used that github issue (https://github.com/dotnet/extensions/issues/5763)
- It did not occur to me to implement change password or password reset. (Yet again it is an in-memory store).
- I would have liked to provide a proper swagger with all the endpoints, contracts, and examples.
- I also did not have the time to sort out the mess in Program.cs and the duplicate code for setting up resilience for every httpclient
- I'd like to add more meaningful filters so that a better response can be provided.
- Solve all the warnings
- Write a better README.md (going more into implementation details and why things are the way they are).
- Many more