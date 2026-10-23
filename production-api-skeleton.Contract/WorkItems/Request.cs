namespace production_api_skeleton.Contract.WorkItems;

public class Request
{
    // public class CreateWordItemRequest
    // {
    //     public required string Title { get; set; }
    // }
    //Đây là một public property
    // required và có thể set sau khi khởi tạo
    
    //public class CreateWordItemRequest(string Title)
    //  Trường hợp này không tự sinh property nên 
    //  VD: var request = new CreateWordItemRequest(string Title);
    //      request cannot call Title
    
    public record CreateWorkItemRequest(string Title);    
    public record UpdateWorkItemStatusRequest(WorkItemStatusRequest Status);
    
    public enum WorkItemStatusRequest 
    { 
        Pending, 
        InProgress, 
        Completed 
    }
}