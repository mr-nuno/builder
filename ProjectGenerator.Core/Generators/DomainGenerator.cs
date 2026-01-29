using ProjectGenerator.Core.Models;

namespace ProjectGenerator.Core.Generators;

public class DomainGenerator : BaseGenerator
{
    public override GenerationResult Generate(GenerationContext context)
    {
        var result = new GenerationResult();
        var projectName = context.ProjectConfig.ProjectName;
        
        // Generate base entity
        GenerateBaseEntity(context, result, projectName);
        
        // Generate entities
        foreach (var entity in context.DomainModel.Entities)
        {
            GenerateEntity(context, result, entity, projectName);
            GenerateSpecifications(context, result, entity, projectName);
        }
        
        return result;
    }
    
    private void GenerateBaseEntity(GenerationContext context, GenerationResult result, string projectName)
    {
        var template = @"namespace {{ project_name }}.Domain;

public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}";
        
        var content = TemplateEngine.Render(template, new { project_name = projectName });
        var path = $"src/{projectName}.Domain/BaseEntity.cs";
        WriteFile(path, content, context, result);
    }
    
    private void GenerateEntity(GenerationContext context, GenerationResult result, EntityDefinition entity, string projectName)
    {
        var properties = new System.Text.StringBuilder();
        
        foreach (var prop in entity.Properties)
        {
            var nullable = prop.IsNullable ? "?" : "";
            var defaultValue = "";
            
            if (!string.IsNullOrEmpty(prop.DefaultValue))
            {
                defaultValue = $" = {GetDefaultValue(prop)}";
            }
            else if (prop.Type == "string" && !prop.IsNullable)
            {
                defaultValue = " = string.Empty";
            }
            else if (prop.Type == "bool" && !prop.IsNullable)
            {
                defaultValue = " = false";
            }
            else if (prop.Type == "int" && !prop.IsNullable)
            {
                defaultValue = " = 0";
            }
            
            properties.AppendLine($"    public {prop.Type}{nullable} {prop.Name} {{ get; set; }}{defaultValue};");
        }
        
        var template = @"namespace {{ project_name }}.Domain.{{ entity_name }};

public class {{ entity_name }} : BaseEntity
{
{{ properties }}
}";
        
        var content = TemplateEngine.Render(template, new 
        { 
            project_name = projectName,
            entity_name = entity.Name,
            properties = properties.ToString().TrimEnd()
        });
        
        var path = $"src/{projectName}.Domain/{entity.Name}/{entity.Name}.cs";
        WriteFile(path, content, context, result);
    }
    
    private string GetDefaultValue(PropertyDefinition prop)
    {
        if (prop.Type == "string")
            return $"\"{prop.DefaultValue}\"";
        if (prop.Type == "bool")
            return prop.DefaultValue.ToLower();
        return prop.DefaultValue ?? "0";
    }
    
    private void GenerateSpecifications(GenerationContext context, GenerationResult result, EntityDefinition entity, string projectName)
    {
        // Generate ById specification
        var byIdTemplate = @"using Ardalis.Specification;
using {{ project_name }}.Domain.{{ entity_name }};

namespace {{ project_name }}.Domain.{{ entity_name }}.Specifications;

public class {{ entity_name }}ByIdSpec : Specification<{{ entity_name }}>
{
    public {{ entity_name }}ByIdSpec(int id) 
    {
        Query.Where(x => x.Id == id);
    }
}";
        
        var byIdContent = TemplateEngine.Render(byIdTemplate, new 
        { 
            project_name = projectName,
            entity_name = entity.Name
        });
        
        var byIdPath = $"src/{projectName}.Domain/{entity.Name}/Specifications/{entity.Name}ByIdSpec.cs";
        WriteFile(byIdPath, byIdContent, context, result);
        
        // Generate All specification
        var allTemplate = @"using Ardalis.Specification;
using {{ project_name }}.Domain.{{ entity_name }};

namespace {{ project_name }}.Domain.{{ entity_name }}.Specifications;

public class {{ entity_name }}AllSpec : Specification<{{ entity_name }}>
{
    public {{ entity_name }}AllSpec() 
    {
        // No filters - returns all
    }
}";
        
        var allContent = TemplateEngine.Render(allTemplate, new 
        { 
            project_name = projectName,
            entity_name = entity.Name
        });
        
        var allPath = $"src/{projectName}.Domain/{entity.Name}/Specifications/{entity.Name}AllSpec.cs";
        WriteFile(allPath, allContent, context, result);
    }
}
