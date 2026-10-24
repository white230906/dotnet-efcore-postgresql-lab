using Microsoft.EntityFrameworkCore;
using DotNetEfCorePostgreSqlLab.Domain.WorkItems;

namespace DotNetEfCorePostgreSqlLab.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options): DbContext(options)
{
    public DbSet<WorkItem> WorkItems => Set<WorkItem>();
    public DbSet<WorkItemStatusHistory> WorkItemStatusHistories => Set<WorkItemStatusHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
    /*
     AppDbContext 
      → quét assembly Persistence 
      → tìm class implement IEntityTypeConfiguration<T> 
      → tự động áp dụng mapping
     Nhờ đó DbContext không chứa hàng loạt cấu hình dài.
     */
}