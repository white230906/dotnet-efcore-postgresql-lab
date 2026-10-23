using Carter;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using production_api_skeleton.Contract.WorkItems;
using production_api_skeleton.Presentation.Abstractions;
using WorkItemService = production_api_skeleton.Application.WorkItems.IService;

namespace production_api_skeleton.Presentation.Apis;

public class WorkItem: ApiEndpoint, ICarterModule
{
    private const string Route = "/api/v{version:apiVersion}/work-items";
    //thằng swagger explore sẽ lấy dữ liệu đẹp hơn từ chỗ này, explore của thằng Carter
    //cộng thêm nữa là DTO khá rõ ràng nên được như vậy
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var versionedApi = app.NewVersionedApi("WorkItems");
        var group = versionedApi
            .MapGroup(Route)
            .HasApiVersion(1);

        group.MapGet("", GetAll)
            .WithName("GetWorkItems")
            .Produces<IReadOnlyCollection<Response.WorkItemResponse>>();

        group.MapGet("{id:guid}", GetById)
            .WithName("GetWorkItemById")
            .Produces<Response.WorkItemResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("", Create)
            .WithName("CreateWorkItem")
            .Produces<Response.WorkItemResponse>(
                StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPut("{id:guid}/status", UpdateStatus)
            .WithName("UpdateWorkItemStatus")
            .Produces<Response.WorkItemResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

    }
    
    private static IResult GetAll(WorkItemService service)
    {
        return Results.Ok(service.GetAll());
    }
    
    private static IResult GetById(
            Guid id,
            WorkItemService service)
        {
            var result = service.GetById(id);
 
            return result is null
                ? NotFound(
                    "WorkItem.NotFound",
                    $"Work item '{id}' does not exist.")
                : Results.Ok(result);
        }
 
    private static IResult Create(
        Request.CreateWorkItemRequest request,
        WorkItemService service)
    {
        var result = service.Create(request);

        return Results.Created(
            $"/api/v1/work-items/{result.Id}",
            result);
    }

    private static IResult UpdateStatus(
        Guid id,
        Request.UpdateWorkItemStatusRequest request,
        WorkItemService service)
    {
        var result = service.UpdateStatus(id, request);

        return result is null
            ? NotFound(
                "WorkItem.NotFound",
                $"Work item '{id}' does not exist.")
            : Results.Ok(result);
    }
    
    
}