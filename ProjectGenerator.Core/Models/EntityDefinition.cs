namespace ProjectGenerator.Core.Models;

public class EntityDefinition
{
    public string Name { get; set; } = string.Empty;
    public List<PropertyDefinition> Properties { get; set; } = new();
}
