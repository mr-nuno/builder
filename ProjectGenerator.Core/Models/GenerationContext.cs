namespace ProjectGenerator.Core.Models;

public class GenerationContext
{
    public bool IsDryRun { get; set; }
    public string OutputDirectory { get; set; } = string.Empty;
    public DomainModel DomainModel { get; set; } = new();
    public ProjectConfig ProjectConfig { get; set; } = new();
}
