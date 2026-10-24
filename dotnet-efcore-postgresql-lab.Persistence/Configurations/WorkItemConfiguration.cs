using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DotNetEfCorePostgreSqlLab.Domain.WorkItems;

namespace DotNetEfCorePostgreSqlLab.Persistence.Configurations;

public class WorkItemConfiguration: IEntityTypeConfiguration<WorkItem>
{
    public void Configure(EntityTypeBuilder<WorkItem> builder)
    {
        builder.ToTable("WorkItems");
        
        builder.HasKey(workItem => workItem.Id); 
 
        builder.Property(workItem => workItem.Id) 
            .ValueGeneratedNever(); 
 
        builder.Property(workItem => workItem.Title) 
            .HasMaxLength(200) 
            .IsRequired(); 
 
        builder.Property(workItem => workItem.Status) 
            .HasConversion<string>() 
            .HasMaxLength(32) 
            .IsRequired(); 
 
        builder.Property(workItem => workItem.CreatedAt) 
            .IsRequired(); 
 
        builder.Property(workItem => workItem.Version) 
            .IsRowVersion();

    }
}