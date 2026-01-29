namespace ProjectGenerator.Core.Models;

public class GenerationResult
{
    public List<FilePreview> Files { get; set; } = new();
    public List<string> Folders { get; set; } = new();
    public Dictionary<string, object> Statistics { get; set; } = new();
    public List<string> GitHubOperations { get; set; } = new();
}
