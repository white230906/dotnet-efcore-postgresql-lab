using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using DotNetEfCorePostgreSqlLab.Contract.WorkItems;
using DotNetEfCorePostgreSqlLab.Domain.Abstractions.Repositories;
using DotNetEfCorePostgreSqlLab.Domain.Exceptions;
using DotNetEfCorePostgreSqlLab.Domain.WorkItems;

namespace DotNetEfCorePostgreSqlLab.Application.WorkItems;

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
        var item = WorkItem.Create(request.Title, _options.MaxTitleLength, DateTimeOffset.UtcNow);
 
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
        
        if (item.Version != request.Version) 
        { 
            throw new ConcurrencyConflictException("The work item was changed by another request. " + 
                                                   "Reload the latest data and try again."); 
        }
        
        var previousStatus = item.Status; 
        var newStatus = ToDomainStatus(request.Status); 
 
        item.ChangeStatus(newStatus);
        
        //Không thay đổi thật sự thì không tạo history. 
        if (previousStatus == item.Status) 
        { 
            return ToResponse(item); 
        } 
        
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
 
        try 
        { 
            //Lưu trạng thái mới. 
            // await _unitOfWork.SaveChangesAsync(cancellationToken); 
 
            var history = WorkItemStatusHistory.Create( 
                item.Id, 
                previousStatus, 
                item.Status, 
                DateTimeOffset.UtcNow); 
 
            await _repository.AddHistoryAsync(history, cancellationToken); 
 
            // Lưu history. 
            await _unitOfWork.SaveChangesAsync(cancellationToken); 
 
            await _unitOfWork.CommitAsync(cancellationToken); 
 
            return ToResponse(item); 
        } 
        catch 
        { 
            await _unitOfWork.RollbackAsync(CancellationToken.None); 
 
            throw; 
        }
        
    }

    public async Task<IReadOnlyCollection<Response.WorkItemStatusHistoryResponse>?> GetHistoryAsync(Guid workItemId, CancellationToken cancellationToken)
    {
        var workItem = await _repository.GetByIdAsync(workItemId, trackChanges: false, cancellationToken); 
 
        if (workItem is null) 
        { 
            return null; 
        } 
 
        var histories = await _repository.GetHistoryAsync(workItemId, cancellationToken); 
 
        return histories 
            .Select(ToHistoryResponse) 
            .ToArray();
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
            item.CreatedAt.UtcDateTime,
            item.Version);
    }
    
    private static Response.WorkItemStatusHistoryResponse ToHistoryResponse(WorkItemStatusHistory history) 
    { 
        return new Response.WorkItemStatusHistoryResponse( 
            history.Id, 
            history.WorkItemId, 
            history.PreviousStatus.ToString(), 
            history.NewStatus.ToString(), 
            history.ChangedAt.UtcDateTime); 
    }
}
