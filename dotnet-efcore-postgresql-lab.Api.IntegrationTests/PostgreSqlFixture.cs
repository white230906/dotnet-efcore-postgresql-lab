using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DotNetEfCorePostgreSqlLab.Persistence;
using Testcontainers.PostgreSql;
using Xunit;

namespace DotNetEfCorePostgreSqlLab.Api.IntegrationTests;

public class PostgreSqlFixture: IAsyncLifetime
{
    
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine") 
        .WithDatabase("work_items_tests") 
        .WithUsername("postgres") 
        .WithPassword("postgres") 
        .Build();
    
    public CustomWebApplicationFactory Factory { get; private set; } = null!;
    public HttpClient Client { get; private set; } = null!;
    
    public async Task InitializeAsync()
    {
        await _container.StartAsync(); 
 
        Factory = new CustomWebApplicationFactory(_container.GetConnectionString()); 
 
        Client = Factory.CreateClient(); 
 
        await using var scope = Factory.Services.CreateAsyncScope(); 
 
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>(); 
 
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        Client.Dispose(); 
        Factory.Dispose(); 
 
        await _container.DisposeAsync();
    }
}