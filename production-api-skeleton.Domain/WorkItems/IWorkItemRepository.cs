namespace production_api_skeleton.Domain.WorkItems;

public interface IWorkItemRepository
{
    Task<IReadOnlyCollection<WorkItem>> GetAllAsync(CancellationToken cancellationToken); 
 
    Task<WorkItem?> GetByIdAsync(Guid id, bool trackChanges, CancellationToken cancellationToken); 
 
    Task AddAsync(WorkItem workItem, CancellationToken cancellationToken);
}