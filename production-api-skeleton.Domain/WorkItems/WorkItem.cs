namespace production_api_skeleton.Domain.WorkItems;

public class WorkItem
{
    private WorkItem(
        Guid id,
        string title,
        WorkItemStatus status,
        DateTimeOffset createAt)
    {
        Id = id;
        Title = title;
        Status = status;
        CreatedAt = createAt;
    }
    
    public Guid Id { get; }
    public string Title { get; private set; }
    public WorkItemStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; }

    public static WorkItem Create(string title, int maxTitleLength, DateTimeOffset createdAt)
    {
        if (string.IsNullOrWhiteSpace(title)) 
        { 
            throw new ArgumentException( 
                "Work item title is required.", 
                nameof(title)); 
        } 
 
        var normalizedTitle = title.Trim(); 
 
        if (normalizedTitle.Length < 3 || 
            normalizedTitle.Length > maxTitleLength) 
        { 
            throw new ArgumentException( 
                $"Title length must be between 3 and {maxTitleLength}.", 
                nameof(title)); 
        } 
 
        return new WorkItem( 
            Guid.NewGuid(), 
            normalizedTitle, 
            WorkItemStatus.Pending, 
            createdAt);
    }
    
    public void ChangeStatus(WorkItemStatus newStatus)
    {
        if (Status == newStatus)
        {
            return;
        }
        //switch express + tuple pattern
        var isValidTransition = (Status, newStatus) switch
        {
            (WorkItemStatus.Pending, WorkItemStatus.InProgress) => true,
            (WorkItemStatus.InProgress, WorkItemStatus.Completed) => true,
            _ => false
        };

        if (!isValidTransition)
        {
            throw new InvalidOperationException(
                $"Cannot change status from {Status} to {newStatus}.");
        }
        
        Status = newStatus;
    }
    
}