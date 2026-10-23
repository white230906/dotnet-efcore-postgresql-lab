using Carter;
using production_api_skeleton.API.Middleware;
using Serilog;

namespace production_api_skeleton.API.DependencyInjection;

public static class ApiApplicationBuilderExtensions
{
    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        // Gắn mã định danh cho mỗi request.
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseSerilogRequestLogging();
        // Bắt exception từ các middleware/endpoint phía sau.
        app.UseMiddleware<ExceptionHandlingMiddleware>();
 
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();//use cái đẹp
            app.UseSwaggerUI();
        }
 
        // Map tất cả Carter module từ Presentation.
        app.MapCarter();
 
        app.MapGet("/health", () =>
            Results.Ok(new
            {
                Status = "Healthy",
                Timestamp = DateTimeOffset.UtcNow
            }));
 
        return app;
    }
}