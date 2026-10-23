namespace production_api_skeleton.Contract.WorkItems;

public class Response
{
    public record WorkItemResponse(
        Guid Id,
        string Title,
        string Status,
        DateTime CreatedAt);
    
    
}