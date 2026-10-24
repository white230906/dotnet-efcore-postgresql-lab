using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace DotNetEfCorePostgreSqlLab.Api.IntegrationTests;
//Factor này thay connection string local bằng connection string của PostgreSQL container
public class CustomWebApplicationFactory: WebApplicationFactory<Program>
{
    private readonly string _connectionString; 
    
    public CustomWebApplicationFactory(string connectionString)
    {
        _connectionString = connectionString;   
    }
    
    protected override void ConfigureWebHost(IWebHostBuilder builder) 
    { 
        builder.UseEnvironment("Testing"); 
 
        builder.ConfigureAppConfiguration( 
            (_, configuration) => 
            { 
                configuration.AddInMemoryCollection(new Dictionary<string, string?> 
                    { 
                        ["ConnectionStrings:PostgresSql"] = _connectionString 
                    }); 
            }); 
    }
}