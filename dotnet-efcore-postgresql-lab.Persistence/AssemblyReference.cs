using System.Reflection;

namespace DotNetEfCorePostgreSqlLab.Persistence;

public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
    // Class này giúp DbContext tìm toàn bộ configuration trong project Persistence
}