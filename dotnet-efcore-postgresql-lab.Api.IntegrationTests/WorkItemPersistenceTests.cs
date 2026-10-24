using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DotNetEfCorePostgreSqlLab.Domain.Abstractions.Repositories;
using DotNetEfCorePostgreSqlLab.Domain.WorkItems;
using DotNetEfCorePostgreSqlLab.Persistence;
using Xunit;

namespace DotNetEfCorePostgreSqlLab.Api.IntegrationTests;

public sealed class WorkItemPersistenceTests
    : IClassFixture<PostgreSqlFixture>
{
    private const string BaseUrl = "/api/v1/work-items";

    private readonly PostgreSqlFixture _fixture;
    private readonly HttpClient _client;

    public WorkItemPersistenceTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Client;
    }

    [Fact]
    public async Task UpdateStatus_PersistsHistory()
    {
        var created = await CreateWorkItemAsync(
            $"History test {Guid.NewGuid():N}");

        var updateResponse = await _client.PutAsJsonAsync(
            $"{BaseUrl}/{created.Id}/status",
            new
            {
                Status = "InProgress",
                Version = created.Version
            });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var historyResponse = await _client.GetAsync(
            $"{BaseUrl}/{created.Id}/history");

        Assert.Equal(HttpStatusCode.OK, historyResponse.StatusCode);

        var histories = await historyResponse.Content
            .ReadFromJsonAsync<List<WorkItemStatusHistoryResponse>>();

        var history = Assert.Single(Assert.IsType<
            List<WorkItemStatusHistoryResponse>>(histories));

        Assert.Equal(created.Id, history.WorkItemId);
        Assert.Equal("Pending", history.PreviousStatus);
        Assert.Equal("InProgress", history.NewStatus);
    }

    [Fact]
    public async Task UpdateStatus_WithStaleVersion_Returns409AndDoesNotOverwriteData()
    {
        var created = await CreateWorkItemAsync(
            $"Stale version test {Guid.NewGuid():N}");

        var firstUpdateResponse = await _client.PutAsJsonAsync(
            $"{BaseUrl}/{created.Id}/status",
            new
            {
                Status = "InProgress",
                Version = created.Version
            });

        Assert.Equal(HttpStatusCode.OK, firstUpdateResponse.StatusCode);

        var firstUpdate = await firstUpdateResponse.Content
            .ReadFromJsonAsync<WorkItemResponse>();

        Assert.NotNull(firstUpdate);
        Assert.NotEqual(created.Version, firstUpdate.Version);

        var staleUpdateResponse = await _client.PutAsJsonAsync(
            $"{BaseUrl}/{created.Id}/status",
            new
            {
                Status = "Completed",
                Version = created.Version
            });

        Assert.Equal(HttpStatusCode.Conflict, staleUpdateResponse.StatusCode);

        using var problem = JsonDocument.Parse(
            await staleUpdateResponse.Content.ReadAsStringAsync());

        Assert.Equal(
            "WorkItem.ConcurrencyConflict",
            problem.RootElement.GetProperty("errorCode").GetString());

        var currentResponse = await _client.GetAsync(
            $"{BaseUrl}/{created.Id}");

        var current = await currentResponse.Content
            .ReadFromJsonAsync<WorkItemResponse>();

        Assert.NotNull(current);
        Assert.Equal("InProgress", current.Status);
        Assert.Equal(firstUpdate.Version, current.Version);
    }

    [Fact]
    public async Task TwoDbContexts_UpdateSameVersion_SecondSaveThrowsConcurrencyException()
    {
        var created = await CreateWorkItemAsync(
            $"DbContext concurrency test {Guid.NewGuid():N}");

        await using var firstScope =
            _fixture.Factory.Services.CreateAsyncScope();

        await using var secondScope =
            _fixture.Factory.Services.CreateAsyncScope();

        var firstDbContext = firstScope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var secondDbContext = secondScope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var firstItem = await firstDbContext.WorkItems
            .SingleAsync(item => item.Id == created.Id);

        var secondItem = await secondDbContext.WorkItems
            .SingleAsync(item => item.Id == created.Id);

        Assert.Equal(firstItem.Version, secondItem.Version);

        firstItem.ChangeStatus(WorkItemStatus.InProgress);
        secondItem.ChangeStatus(WorkItemStatus.InProgress);

        await firstDbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => secondDbContext.SaveChangesAsync());
    }

    [Fact]
    public async Task Transaction_WhenFailureOccurs_RollsBackWorkItemChange()
    {
        var created = await CreateWorkItemAsync(
            $"Rollback test {Guid.NewGuid():N}");

        await using (var scope =
                     _fixture.Factory.Services.CreateAsyncScope())
        {
            var repository = scope.ServiceProvider
                .GetRequiredService<IWorkItemRepository>();

            var unitOfWork = scope.ServiceProvider
                .GetRequiredService<IUnitOfWork>();

            await unitOfWork.BeginTransactionAsync(
                CancellationToken.None);

            try
            {
                var item = await repository.GetByIdAsync(
                    created.Id,
                    trackChanges: true,
                    CancellationToken.None);

                Assert.NotNull(item);

                item.ChangeStatus(WorkItemStatus.InProgress);

                await unitOfWork.SaveChangesAsync(
                    CancellationToken.None);

                throw new InvalidOperationException(
                    "Simulated failure after saving the work item.");
            }
            catch (InvalidOperationException)
            {
                await unitOfWork.RollbackAsync(
                    CancellationToken.None);
            }
        }

        await using var verificationScope =
            _fixture.Factory.Services.CreateAsyncScope();

        var verificationRepository = verificationScope.ServiceProvider
            .GetRequiredService<IWorkItemRepository>();

        var persistedItem = await verificationRepository.GetByIdAsync(
            created.Id,
            trackChanges: false,
            CancellationToken.None);

        var histories = await verificationRepository.GetHistoryAsync(
            created.Id,
            CancellationToken.None);

        Assert.NotNull(persistedItem);
        Assert.Equal(WorkItemStatus.Pending, persistedItem.Status);
        Assert.Empty(histories);
    }

    private async Task<WorkItemResponse> CreateWorkItemAsync(string title)
    {
        var response = await _client.PostAsJsonAsync(
            BaseUrl,
            new { Title = title });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<WorkItemResponse>();

        return Assert.IsType<WorkItemResponse>(result);
    }

    private sealed record WorkItemResponse(
        Guid Id,
        string Title,
        string Status,
        DateTime CreatedAt,
        uint Version);

    private sealed record WorkItemStatusHistoryResponse(
        Guid Id,
        Guid WorkItemId,
        string PreviousStatus,
        string NewStatus,
        DateTime ChangedAt);
}
