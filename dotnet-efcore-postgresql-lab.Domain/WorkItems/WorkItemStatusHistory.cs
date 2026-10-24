namespace DotNetEfCorePostgreSqlLab.Domain.WorkItems;

public class WorkItemStatusHistory
{
    private WorkItemStatusHistory(
        Guid id,
        Guid workItemId,
        WorkItemStatus previousStatus,
        WorkItemStatus newStatus,
        DateTimeOffset changedAt)
    {
        Id = id;
        WorkItemId = workItemId;
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
        ChangedAt = changedAt;
    }
    
    public Guid Id { get; } 
 
    public Guid WorkItemId { get; } 
 
    public WorkItemStatus PreviousStatus { get; } 
 
    public WorkItemStatus NewStatus { get; } 
 
    public DateTimeOffset ChangedAt { get; }
    
    public static WorkItemStatusHistory Create( 
        Guid workItemId, 
        WorkItemStatus previousStatus, 
        WorkItemStatus newStatus, 
        DateTimeOffset changedAt) 
    { 
        if (workItemId == Guid.Empty) 
        { 
            throw new ArgumentException( 
                "Work item ID is required.", 
                nameof(workItemId)); 
        } 
 
        if (previousStatus == newStatus) 
        { 
            throw new InvalidOperationException( 
                "A status history requires an actual status change."); 
        } 
 
        return new WorkItemStatusHistory(Guid.NewGuid(), workItemId, previousStatus, newStatus, changedAt); 
    }
    
}