using Carter;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using DotNetEfCorePostgreSqlLab.Contract.WorkItems;
using DotNetEfCorePostgreSqlLab.Presentation.Abstractions;
using WorkItemService = DotNetEfCorePostgreSqlLab.Application.WorkItems.IService;

namespace DotNetEfCorePostgreSqlLab.Presentation.Apis;

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
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
        
        group.MapGet("{id:guid}/history", GetHistory) 
            .WithName("GetWorkItemStatusHistory") 
            .Produces< 
                IReadOnlyCollection< 
                    Response.WorkItemStatusHistoryResponse>>() 
            .ProducesProblem(StatusCodes.Status404NotFound);

    }
    
    private static async Task<IResult> GetAll(WorkItemService service, CancellationToken cancellationToken) 
    { 
        var result = await service.GetAllAsync(cancellationToken); 
 
        return Results.Ok(result);
    }

    private static async Task<IResult> GetById(Guid id, WorkItemService service, CancellationToken cancellationToken)
    {
        var result = await service.GetByIdAsync(id, cancellationToken);

        return result is null
            ? NotFound(
                "WorkItem.NotFound",
                $"Work item '{id}' does not exist.")
            : Results.Ok(result);
    }

    private static async Task<IResult> Create(Request.CreateWorkItemRequest request, WorkItemService service, CancellationToken cancellationToken) 
          { 
              var result = await service.CreateAsync( 
                  request, 
                  cancellationToken); 
 
              return Results.Created( 
                  $"/api/v1/work-items/{result.Id}", 
                  result); 
          }

    private static async Task<IResult> UpdateStatus(Guid id, Request.UpdateWorkItemStatusRequest request, 
                                                        WorkItemService service, 
                                                        CancellationToken cancellationToken) 
    { 
        var result = await service.UpdateStatusAsync(id, request, cancellationToken); 
 
        return result is null 
            ? NotFound( 
                "WorkItem.NotFound", 
                $"Work item '{id}' does not exist.") 
            : Results.Ok(result); 
    }
    
    private static async Task<IResult> GetHistory(Guid id, WorkItemService service, CancellationToken cancellationToken) 
    { 
        var result = await service.GetHistoryAsync(id, cancellationToken); 
 
        return result is null 
            ? NotFound( 
                "WorkItem.NotFound", 
                $"Work item '{id}' does not exist.") 
            : Results.Ok(result); 
    }
}