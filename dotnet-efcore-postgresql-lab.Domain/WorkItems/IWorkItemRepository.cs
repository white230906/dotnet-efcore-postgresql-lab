namespace DotNetEfCorePostgreSqlLab.Domain.WorkItems;

public interface IWorkItemRepository
{
    Task<IReadOnlyCollection<WorkItem>> GetAllAsync(CancellationToken cancellationToken); 
 
    Task<WorkItem?> GetByIdAsync(Guid id, bool trackChanges, CancellationToken cancellationToken); 
 
    Task AddAsync(WorkItem workItem, CancellationToken cancellationToken);
    
    Task AddHistoryAsync(WorkItemStatusHistory history, CancellationToken cancellationToken); 
 
    Task<IReadOnlyCollection<WorkItemStatusHistory>> GetHistoryAsync(Guid workItemId, CancellationToken cancellationToken);
}