using production_api_skeleton.API.DependencyInjection;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
//Serilog
//(...) => { ... }    → lambda expression/callback
//.Method().Method()  → fluent API/method chaining
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration) //đọc log level từ appsettings.json.
        .ReadFrom.Services(services)//cho phép Serilog sử dụng dependency từ DI.
        .Enrich.FromLogContext()//nhận thêm CorrelationId từ logging scope.
        .WriteTo.Console();//in log ra terminal.
});
/*
private static void ConfigureSerilog(
      HostBuilderContext context,
      IServiceProvider services,
      LoggerConfiguration configuration)
  {
      // Cấu hình
  }
 */

builder.Services.AddApiServices();
 
var app = builder.Build(); 

app.UseApiPipeline();
 
app.Run(); 
 
// Cho phép WebApplicationFactory truy cập khi viết integration test.public partial class Program 
public partial class Program;