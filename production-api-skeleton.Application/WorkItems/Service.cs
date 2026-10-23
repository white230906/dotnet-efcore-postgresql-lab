using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using production_api_skeleton.Contract.WorkItems;
using production_api_skeleton.Domain.Abstractions.Repositories;
using production_api_skeleton.Domain.WorkItems;

namespace production_api_skeleton.Application.WorkItems;

public class Service: IService
{
    private readonly IWorkItemRepository _repository; 
    private readonly IUnitOfWork _unitOfWork; 
    private readonly WorkItemOptions _options;
    
    public Service(IOptions<WorkItemOptions> options, IWorkItemRepository repository, IUnitOfWork unitOfWork)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
        _repository = repository; 
        _unitOfWork = unitOfWork; 
    }
    
    public async Task<IReadOnlyCollection<Response.WorkItemResponse>> GetAllAsync(CancellationToken cancellationToken) 
    { 
        var items = await _repository.GetAllAsync(cancellationToken);

        return items
            .Select(ToResponse)
            .ToList();
    }

    public async Task<Response.WorkItemResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = await _repository.GetByIdAsync(id, trackChanges: false, cancellationToken);
        
        return item is null ? null : ToResponse(item);
    }

    public async Task<Response.WorkItemResponse> CreateAsync (Request.CreateWorkItemRequest request, CancellationToken cancellationToken)
    {
        var item = WorkItem.Create(
            request.Title,
            _options.MaxTitleLength,
            DateTimeOffset.UtcNow);
 
        await _repository.AddAsync(item, cancellationToken);
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);
 
        return ToResponse(item);
    }

    public async Task<Response.WorkItemResponse?> UpdateStatusAsync (Guid id, Request.UpdateWorkItemStatusRequest request, CancellationToken cancellationToken)
    {
        
        var item = await _repository.GetByIdAsync(id, trackChanges: true, cancellationToken);

        if (item is null)
        {
            return null;
        }
 
        var newStatus = ToDomainStatus(request.Status);
        
        item.ChangeStatus(newStatus);
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return ToResponse(item);
        
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