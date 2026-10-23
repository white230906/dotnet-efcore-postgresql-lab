using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace production_api_skeleton.Api.IntegrationTests;

public class WorkItemApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string BaseUrl = "/api/v1/work-items";
    private const string CorrelationHeader = "X-Correlation-ID";
    
    private readonly HttpClient _client;

    public WorkItemApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }
    
    [Fact]
    public async Task Create_WithValidTitle_Returns201Created()
    {
        // Arrange
        var request = new
        {
            Title = "Learn integration testing"
        };

        // Act
        var response = await _client.PostAsJsonAsync(
            "/api/v1/work-items",
            request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.NotEqual(Guid.Empty, body.GetProperty("id").GetGuid());
        Assert.Equal(
            request.Title,
            body.GetProperty("title").GetString());

        Assert.Equal(
            "Pending",
            body.GetProperty("status").GetString());
    }
    
    [Fact]
    public async Task Create_WithInvalidTitle_Returns422ProblemDetails()
    {
        var request = new
        {
            Title = "AB"
        };
 
        var response = await _client.PostAsJsonAsync(BaseUrl, request);
 
        Assert.Equal(
            HttpStatusCode.UnprocessableEntity,
            response.StatusCode);
 
        Assert.Equal(
            "application/json",
            response.Content.Headers.ContentType?.MediaType);
 
        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());
 
        var body = document.RootElement;
 
        Assert.Equal(422, body.GetProperty("status").GetInt32());
        Assert.Equal(
            "Validation error",
            body.GetProperty("title").GetString());
 
        Assert.Equal(
            "Request.ValidationFailed",
            body.GetProperty("errorCode").GetString());
    }
 
    [Fact]
    public async Task GetById_WithExistingId_ReturnsWorkItem()
    {
        var createdItem = await CreateWorkItemAsync(
            "Work item for GET test");
 
        var response = await _client.GetAsync(
            $"{BaseUrl}/{createdItem.Id}");
 
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
 
        var result =
            await response.Content.ReadFromJsonAsync<WorkItemResponse>();
 
        Assert.NotNull(result);
        Assert.Equal(createdItem.Id, result.Id);
        Assert.Equal(createdItem.Title, result.Title);
        Assert.Equal("Pending", result.Status);
    }
    
    [Fact]
    public async Task GetById_WithUnknownId_Returns404ProblemDetails()
    {
        var unknownId = Guid.NewGuid();
 
        var response = await _client.GetAsync(
            $"{BaseUrl}/{unknownId}");
 
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
 
        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());
 
        var body = document.RootElement;
 
        Assert.Equal(404, body.GetProperty("status").GetInt32());
        Assert.Equal(
            "WorkItem.NotFound",
            body.GetProperty("errorCode").GetString());
    }
 
    [Fact]
    public async Task GetAll_ReturnsCreatedWorkItem()
    {
        var createdItem = await CreateWorkItemAsync(
            "Work item for GET ALL test");
 
        var response = await _client.GetAsync(BaseUrl);
 
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
 
        var result =
            await response.Content.ReadFromJsonAsync<List<WorkItemResponse>>();
 
        Assert.NotNull(result);
        Assert.Contains(
            result,
            item => item.Id == createdItem.Id);
    }
    
    [Fact]
    public async Task UpdateStatus_WithValidTransition_ReturnsUpdatedWorkItem()
    {
        var createdItem = await CreateWorkItemAsync(
            "Work item for update test");
 
        var request = new
        {
            Status = "InProgress"
        };
 
        var response = await _client.PutAsJsonAsync(
            $"{BaseUrl}/{createdItem.Id}/status",
            request);
 
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
 
        var result =
            await response.Content.ReadFromJsonAsync<WorkItemResponse>();
 
        Assert.NotNull(result);
        Assert.Equal(createdItem.Id, result.Id);
        Assert.Equal("InProgress", result.Status);
    }
 
    [Fact]
    public async Task UpdateStatus_WithInvalidTransition_Returns409Conflict()
    {
        var createdItem = await CreateWorkItemAsync(
            "Work item for conflict test");
 
        var request = new
        {
            Status = "Completed"
        };
 
        var response = await _client.PutAsJsonAsync(
            $"{BaseUrl}/{createdItem.Id}/status",
            request);
 
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
 
        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());
 
        var body = document.RootElement;
 
        Assert.Equal(409, body.GetProperty("status").GetInt32());
        Assert.Equal(
            "WorkItem.InvalidState",
            body.GetProperty("errorCode").GetString());
    }
    
    [Fact]
    public async Task UpdateStatus_WithUnknownId_Returns404()
    {
        var request = new
        {
            Status = "InProgress"
        };
 
        var response = await _client.PutAsJsonAsync(
            $"{BaseUrl}/{Guid.NewGuid()}/status",
            request);
 
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
 
    [Fact]
    public async Task Request_WithProvidedCorrelationId_ReturnsSameId()
    {
        const string correlationId = "integration-test-123";
 
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            BaseUrl);
 
        request.Headers.Add(
            CorrelationHeader,
            correlationId);
 
        var response = await _client.SendAsync(request);
 
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
 
        Assert.True(
            response.Headers.TryGetValues(
                CorrelationHeader,
                out var values));
 
        Assert.Equal(correlationId, values.Single());
    }
 
    [Fact]
    public async Task Request_WithoutCorrelationId_GeneratesNewId()
    {
        var response = await _client.GetAsync(BaseUrl);
 
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
 
        Assert.True(
            response.Headers.TryGetValues(
                CorrelationHeader,
                out var values));
 
        Assert.False(string.IsNullOrWhiteSpace(values.Single()));
    }
    
    [Fact]
    public async Task Create_WhenRequestsRunConcurrently_ReturnsUniqueIds()
    {
        var requests = Enumerable
            .Range(1, 10)
            .Select(index =>
                CreateWorkItemAsync(
                    $"Concurrent work item {index}"));
 
        var results = await Task.WhenAll(requests);
 
        Assert.Equal(10, results.Length);
        Assert.Equal(
            10,
            results.Select(item => item.Id).Distinct().Count());
    }
 
    private async Task<WorkItemResponse> CreateWorkItemAsync(
        string title)
    {
        var response = await _client.PostAsJsonAsync(
            BaseUrl,
            new { Title = title });
 
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
 
        var result =
            await response.Content.ReadFromJsonAsync<WorkItemResponse>();
 
        return Assert.IsType<WorkItemResponse>(result);
    }
 
    private sealed record WorkItemResponse(
        Guid Id,
        string Title,
        string Status,
        DateTime CreatedAt);
    
}