namespace ProjectGenerator.Core.Models;

public class Epic
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<UserStory> Stories { get; set; } = new();
}
