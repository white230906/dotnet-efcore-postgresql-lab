using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using production_api_skeleton.Domain.WorkItems;

namespace production_api_skeleton.Persistence.Configurations;

public class WorkItemStatusHistoryConfiguration: IEntityTypeConfiguration<WorkItemStatusHistory>
{
    public void Configure(EntityTypeBuilder<WorkItemStatusHistory> builder)
    {
        builder.ToTable("WorkItemStatusHistories"); 
 
        builder.HasKey(history => history.Id); 
 
        builder.Property(history => history.Id) 
            .ValueGeneratedNever(); 
 
        builder.Property(history => history.WorkItemId) 
            .IsRequired(); 
 
        builder.Property(history => history.PreviousStatus) 
            .HasConversion<string>() 
            .HasMaxLength(32) 
            .IsRequired(); 
 
        builder.Property(history => history.NewStatus) 
            .HasConversion<string>() 
            .HasMaxLength(32) 
            .IsRequired(); 
 
        builder.Property(history => history.ChangedAt) 
            .IsRequired(); 
 
        builder.HasOne<WorkItem>() 
            .WithMany() 
            .HasForeignKey(history => history.WorkItemId) 
            .OnDelete(DeleteBehavior.Cascade); 
 
        builder.HasIndex(history => new 
        { 
            history.WorkItemId, 
            history.ChangedAt 
        });
    }
}