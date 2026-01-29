namespace ProjectGenerator.Core.Models;

public class ProjectConfig
{
    public string ProjectName { get; set; } = string.Empty;
    public string FrameworkVersion { get; set; } = "8.0";
    public string Database { get; set; } = "SQL Server";
    public string Orm { get; set; } = "Entity Framework Core";
    public List<Epic> Epics { get; set; } = new();
    public List<UserStory> Stories { get; set; } = new();
}
