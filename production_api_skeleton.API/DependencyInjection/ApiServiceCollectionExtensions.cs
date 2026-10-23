using System.Text.Json.Serialization;
using Asp.Versioning;
using Carter;
using production_api_skeleton.Application.WorkItems;
using WorkItemService =
    production_api_skeleton.Application.WorkItems.IService;
using WorkItemServiceImplementation =
    production_api_skeleton.Application.WorkItems.Service;
namespace production_api_skeleton.API.DependencyInjection;

public static class ApiServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddCarter();
        //thằng này làm cho swagger trông đẹp hơn
        services.AddEndpointsApiExplorer();//thu thập metadata từ Minimal API/Carter.
        services.AddSwaggerGen();// chuyển metadata thành tài liệu OpenAPI.
        services.AddProblemDetails();

        services
            .AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1); 
                //đặc version mặc định là v1
                options.AssumeDefaultVersionWhenUnspecified = true;
                //nếu request không chỉ rõ version thì mặc định version 1
                options.ReportApiVersions = true;
                //thêm thông tin các version được hỗ trợ vào response header
                //có thể là 1.0 hoặc 2.0 và ngừng hỗ trợ: 0.9
                //api-supported-versions: 1.0, 2.0
                //api-deprecated-versions: 0.9
            })
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'V";
                options.SubstituteApiVersionInUrl = true;
            });
        // phần này phục vụ: GroupNameFormat tao tên version v1, v2
        //SubstituteApiVersionInUrl: đỏi {version} thành số thật trong Swagger
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(
                new JsonStringEnumConverter());
        });
        //Client có thể gửi: status: "InProgress"
        //Thay vì phải gửi số khó hiểu như là 1
        
        //bind
        services
            .AddOptions<WorkItemOptions>()
            .BindConfiguration(WorkItemOptions.SectionName)
            .Validate(options => options.MaxTitleLength is >= 3 and <= 200, 
                "WorkItems:MaxTitleLength must be between 3 and 200.")
            .ValidateOnStart();
        
        //Singleton để dữ liệu in-memory tồn tại qua nhiều request
        services.AddSingleton<WorkItemService, WorkItemServiceImplementation>();
        
        return services;   
    }
}