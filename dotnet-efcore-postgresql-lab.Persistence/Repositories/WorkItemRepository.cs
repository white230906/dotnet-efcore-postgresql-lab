using Microsoft.EntityFrameworkCore;
using DotNetEfCorePostgreSqlLab.Domain.WorkItems;

namespace DotNetEfCorePostgreSqlLab.Persistence.Repositories;

public class WorkItemRepository: IWorkItemRepository
{
    private readonly AppDbContext _dbContext;
    
    public WorkItemRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    public async Task<IReadOnlyCollection<WorkItem>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.WorkItems 
            .AsNoTracking() 
            .OrderByDescending(item => item.CreatedAt) 
            .ToArrayAsync(cancellationToken);
    }

    public async Task<WorkItem?> GetByIdAsync(Guid id, bool trackChanges, CancellationToken cancellationToken)
    {
        IQueryable<WorkItem> query = _dbContext.WorkItems;

        if (!trackChanges)
        {
            query = query.AsNoTracking();
        }

        return await query.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    public async Task AddAsync(WorkItem workItem, CancellationToken cancellationToken)
    {
        await _dbContext.WorkItems.AddAsync(workItem, cancellationToken);
    }

    public async Task AddHistoryAsync(WorkItemStatusHistory history, CancellationToken cancellationToken)
    {
        await _dbContext.WorkItemStatusHistories.AddAsync(history, cancellationToken);
    }

    public async Task<IReadOnlyCollection<WorkItemStatusHistory>> GetHistoryAsync(Guid workItemId, CancellationToken cancellationToken)
    {
        return await _dbContext.WorkItemStatusHistories 
            .AsNoTracking() 
            .Where(history => history.WorkItemId == workItemId) 
            .OrderBy(history => history.ChangedAt) 
            .ToArrayAsync(cancellationToken);
    }
}