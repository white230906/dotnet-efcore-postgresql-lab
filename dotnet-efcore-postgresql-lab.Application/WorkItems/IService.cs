using DotNetEfCorePostgreSqlLab.Contract.WorkItems;

namespace DotNetEfCorePostgreSqlLab.Application.WorkItems;

public interface IService
{
    Task<IReadOnlyCollection<Response.WorkItemResponse>> GetAllAsync(CancellationToken cancellationToken); 
 
    Task<Response.WorkItemResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken); 
 
    Task<Response.WorkItemResponse> CreateAsync(Request.CreateWorkItemRequest request, CancellationToken cancellationToken); 
 
    Task<Response.WorkItemResponse?> UpdateStatusAsync(Guid id, Request.UpdateWorkItemStatusRequest request, CancellationToken cancellationToken);
    
    Task<IReadOnlyCollection<Response.WorkItemStatusHistoryResponse>?> GetHistoryAsync(Guid workItemId, CancellationToken cancellationToken);
}