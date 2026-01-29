using System.Text.RegularExpressions;
using ProjectGenerator.Core.Models;

namespace ProjectGenerator.Core.Parsers;

public class DomainParser
{
    public DomainModel Parse(string content)
    {
        var model = new DomainModel();
        var lines = content.Split('\n');
        
        EntityDefinition? currentEntity = null;
        
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            
            // Check for entity header (## EntityName)
            var entityMatch = Regex.Match(trimmed, @"^##\s+(.+)$");
            if (entityMatch.Success)
            {
                // Save previous entity if exists
                if (currentEntity != null)
                {
                    model.Entities.Add(currentEntity);
                }
                
                // Start new entity
                currentEntity = new EntityDefinition
                {
                    Name = entityMatch.Groups[1].Value.Trim()
                };
                continue;
            }
            
            // Check for property (- PropertyName (type): constraints)
            if (trimmed.StartsWith("-") && currentEntity != null)
            {
                var property = ParseProperty(trimmed);
                if (property != null)
                {
                    currentEntity.Properties.Add(property);
                }
            }
        }
        
        // Add last entity
        if (currentEntity != null)
        {
            model.Entities.Add(currentEntity);
        }
        
        return model;
    }
    
    private PropertyDefinition? ParseProperty(string line)
    {
        // Format: - PropertyName (type): Constraint1, Constraint2
        // Example: - Title (string): Max 200 tecken, Required
        
        var match = Regex.Match(line, @"^-\s+(\w+)\s+\(([^)]+)\)(?::\s*(.+))?$");
        if (!match.Success)
            return null;
        
        var name = match.Groups[1].Value;
        var typeStr = match.Groups[2].Value.Trim();
        var constraints = match.Groups[3].Value.Trim();
        
        var property = new PropertyDefinition
        {
            Name = name
        };
        
        // Parse type and nullable
        if (typeStr.EndsWith("?"))
        {
            property.IsNullable = true;
            property.Type = typeStr.TrimEnd('?').Trim();
        }
        else
        {
            property.Type = typeStr;
            property.IsNullable = false;
        }
        
        // Normalize type names
        property.Type = NormalizeType(property.Type);
        
        // Parse constraints
        if (!string.IsNullOrEmpty(constraints))
        {
            ParseConstraints(property, constraints);
        }
        
        return property;
    }
    
    private string NormalizeType(string type)
    {
        return type.ToLower() switch
        {
            "string" => "string",
            "int" => "int",
            "bool" => "bool",
            "datetime" => "DateTime",
            "guid" => "Guid",
            "decimal" => "decimal",
            _ => type
        };
    }
    
    private void ParseConstraints(PropertyDefinition property, string constraints)
    {
        var parts = constraints.Split(',');
        
        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            
            if (trimmed.Equals("Required", StringComparison.OrdinalIgnoreCase))
            {
                property.IsRequired = true;
            }
            else if (trimmed.Equals("Unique", StringComparison.OrdinalIgnoreCase))
            {
                property.IsUnique = true;
            }
            else if (trimmed.StartsWith("Max", StringComparison.OrdinalIgnoreCase))
            {
                var maxMatch = Regex.Match(trimmed, @"Max\s+(\d+)");
                if (maxMatch.Success && int.TryParse(maxMatch.Groups[1].Value, out var max))
                {
                    if (property.Type == "string")
                        property.MaxLength = max;
                    else
                        property.MaxValue = max;
                }
            }
            else if (trimmed.StartsWith("Min", StringComparison.OrdinalIgnoreCase))
            {
                var minMatch = Regex.Match(trimmed, @"Min\s+(\d+)");
                if (minMatch.Success && int.TryParse(minMatch.Groups[1].Value, out var min))
                {
                    if (property.Type == "string")
                        property.MinLength = min;
                    else
                        property.MinValue = min;
                }
            }
            else if (trimmed.StartsWith("Range", StringComparison.OrdinalIgnoreCase))
            {
                var rangeMatch = Regex.Match(trimmed, @"Range\s+(\d+)-(\d+)");
                if (rangeMatch.Success)
                {
                    if (int.TryParse(rangeMatch.Groups[1].Value, out var min))
                        property.MinValue = min;
                    if (int.TryParse(rangeMatch.Groups[2].Value, out var max))
                        property.MaxValue = max;
                }
            }
            else if (trimmed.StartsWith("Default", StringComparison.OrdinalIgnoreCase))
            {
                var defaultMatch = Regex.Match(trimmed, @"Default\s+(.+)");
                if (defaultMatch.Success)
                {
                    property.DefaultValue = defaultMatch.Groups[1].Value.Trim();
                }
            }
            else if (trimmed.StartsWith("Format", StringComparison.OrdinalIgnoreCase))
            {
                var formatMatch = Regex.Match(trimmed, @"Format\s+(.+)");
                if (formatMatch.Success)
                {
                    property.Format = formatMatch.Groups[1].Value.Trim();
                }
            }
        }
    }
}
