using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using production_api_skeleton.Contract.WorkItems;
using production_api_skeleton.Domain.WorkItems;

namespace production_api_skeleton.Application.WorkItems;

public class Service: IService
{
 
    private readonly ConcurrentDictionary<Guid, WorkItem> _items = new();
    private readonly WorkItemOptions _options;
    
    public Service(IOptions<WorkItemOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
    }
    
    public IReadOnlyCollection<Response.WorkItemResponse> GetAll()
    {
        return _items.Values
            .OrderByDescending(item => item.CreatedAt)
            .Select(ToResponse)
            .ToArray();
    }

    public Response.WorkItemResponse? GetById(Guid id)
    {
        return _items.TryGetValue(id, out var item)
            ? ToResponse(item)
            : null;
    }

    public Response.WorkItemResponse Create(Request.CreateWorkItemRequest request)
    {
        var item = WorkItem.Create(
            request.Title,
            _options.MaxTitleLength,
            DateTimeOffset.UtcNow);
 
        if (!_items.TryAdd(item.Id, item))
        {
            throw new InvalidOperationException(
                "Could not store the work item.");
        }
 
        return ToResponse(item);
    }

    public Response.WorkItemResponse? UpdateStatus(Guid id, Request.UpdateWorkItemStatusRequest request)
    {
        if (!_items.TryGetValue(id, out var item))
        {
            return null;
        }
 
        var newStatus = ToDomainStatus(request.Status);
 
        lock (item)
        {
            item.ChangeStatus(newStatus);
            return ToResponse(item);
        }
    }
    
    private static WorkItemStatus ToDomainStatus(
        Request.WorkItemStatusRequest status)
    {
        return status switch
        {
            Request.WorkItemStatusRequest.Pending =>
                WorkItemStatus.Pending,
 
            Request.WorkItemStatusRequest.InProgress =>
                WorkItemStatus.InProgress,
 
            Request.WorkItemStatusRequest.Completed =>
                WorkItemStatus.Completed,
 
            _ => throw new ArgumentOutOfRangeException(
                nameof(status),
                status,
                "Unsupported work item status.")
        };
    }
 
    private static Response.WorkItemResponse ToResponse(WorkItem item)
    {
        return new Response.WorkItemResponse(
            item.Id,
            item.Title,
            item.Status.ToString(),
            item.CreatedAt.UtcDateTime);
    }
}