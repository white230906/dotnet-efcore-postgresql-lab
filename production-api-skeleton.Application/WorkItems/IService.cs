using production_api_skeleton.Contract.WorkItems;

namespace production_api_skeleton.Application.WorkItems;

public interface IService
{
    IReadOnlyCollection<Response.WorkItemResponse> GetAll();
    Response.WorkItemResponse? GetById(Guid id);
    Response.WorkItemResponse Create(Request.CreateWorkItemRequest request);
    Response.WorkItemResponse? UpdateStatus(Guid id, Request.UpdateWorkItemStatusRequest request);
}