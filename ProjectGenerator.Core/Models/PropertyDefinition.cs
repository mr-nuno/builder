namespace ProjectGenerator.Core.Models;

public class PropertyDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
    public bool IsRequired { get; set; }
    public int? MaxLength { get; set; }
    public int? MinLength { get; set; }
    public int? MinValue { get; set; }
    public int? MaxValue { get; set; }
    public string? DefaultValue { get; set; }
    public bool IsUnique { get; set; }
    public string? Format { get; set; }
}
