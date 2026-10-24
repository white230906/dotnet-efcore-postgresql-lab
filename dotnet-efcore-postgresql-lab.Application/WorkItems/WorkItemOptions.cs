namespace DotNetEfCorePostgreSqlLab.Application.WorkItems;

public class WorkItemOptions
{
    public const string SectionName = "WorkItems";

    public int MaxTitleLength { get; init; }
}