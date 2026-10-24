using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DotNetEfCorePostgreSqlLab.Domain.Abstractions.Repositories;
using DotNetEfCorePostgreSqlLab.Domain.WorkItems;
using DotNetEfCorePostgreSqlLab.Persistence.Repositories;
using Microsoft.Extensions.Hosting;

namespace DotNetEfCorePostgreSqlLab.Persistence.DependencyInjection;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddPostgreSqlPersistence( 
        this IServiceCollection services, 
        IConfiguration configuration, 
        IHostEnvironment environment) 
    { 
        var connectionString = 
            configuration.GetConnectionString("PostgresSql") 
            ?? throw new InvalidOperationException( 
                "Connection string 'PostgresSql' is missing."); 
 
        services.AddDbContext<AppDbContext>(options => 
        { 
            options.UseNpgsql(connectionString); 
 
            if (environment.IsDevelopment()) 
            { 
                options.EnableDetailedErrors(); 
                //EF core sẽ hiển thị lỗi database chi tiết hơn, giúp tôi debug dễ dàng hơn
                options.EnableSensitiveDataLogging(); 
                //cho phép log của giá trị parameter/dữ liệu thật trong câu query
            } 
        }); 
 
        services.AddScoped<IWorkItemRepository, WorkItemRepository>(); 
 
        services.AddScoped<IUnitOfWork, EfUnitOfWork>(); 
 
        return services; 
    }
}