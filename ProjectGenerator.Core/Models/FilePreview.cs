namespace ProjectGenerator.Core.Models;

public class FilePreview
{
    public string Path { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int LineCount { get; set; }
}
