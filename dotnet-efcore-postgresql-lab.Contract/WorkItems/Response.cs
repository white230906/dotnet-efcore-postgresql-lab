namespace DotNetEfCorePostgreSqlLab.Contract.WorkItems;

public class Response
{
    public record WorkItemResponse(Guid Id, string Title, string Status, DateTime CreatedAt, uint Version);
    
    public record WorkItemStatusHistoryResponse(Guid Id, Guid WorkItemId, string PreviousStatus, string NewStatus, DateTime ChangedAt);
}