# API Aggregation Assignment Service

## Overview
This service is designed to aggregate data from multiple APIs and provide a unified response.
It uses the adapter pattern to integrate with different APIs seamlessly.
All you need to do to add a new API is to create an Adapter implementing the BaseApiClient (that will handle json parsing, fallbacks, exceptions etc.)
You need to also inject the API settings (clientKey etc.) and adapt the request filter to your API needs.
In cases where the API needs something extra like authentication or special headers, you can provide them in your adapter (see spotify).
Using the decorator pattern, we add caching functionality to the API clients without modifying their code.

Current Implemented APIs:
- OpenWeatherMap API: https://openweathermap.org/api
- News API: https://newsapi.org/
- Spotify Web API

I could not come up with a clever idea of combining the entirely different APIs into one single response.
For that reason, I decided to create a simple API that will return the data from the APIs in a single response, and provide some filtering.


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
  "password": "StrongPassword123",
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
- **Description**: Registers a new user.
- **Request Body Example**:
```json
{
  "username": "exampleUser",
  "password": "StrongPassword123",
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
- **Description**: Registers a new user.
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

## Known Bugs:
- Statistics endpoint returns cached Empty Response.

## Final Notes & Observations:
- I tried to tackle each and every requirement in the task description, and I think that finally backfired.
- Given more time I would have made the code more testable and added more unit tests (test coverage is low).
  - For example, some classes have more than one responsibility, some classes use the stopwotch directly, etc.
- I chose Microsoft HybridCache for caching, but it was a bit difficult to set up in tests and I used that github issue (https://github.com/dotnet/extensions/issues/5763)
