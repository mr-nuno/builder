using ProjectGenerator.Core.Models;

namespace ProjectGenerator.Core.Generators;

public class EndpointGenerator : BaseGenerator
{
    public override GenerationResult Generate(GenerationContext context)
    {
        var result = new GenerationResult();
        var projectName = context.ProjectConfig.ProjectName;
        
        // Generate endpoints from features
        foreach (var epic in context.ProjectConfig.Epics)
        {
            foreach (var story in epic.Stories)
            {
                var featureName = ExtractFeatureName(story.Description);
                var entityName = InferEntityFromStory(story.Description, context.DomainModel);
                
                if (!string.IsNullOrEmpty(entityName))
                {
                    if (IsCommand(story.Description))
                    {
                        GenerateCommandEndpoint(context, result, featureName, entityName, projectName);
                    }
                    else
                    {
                        GenerateQueryEndpoint(context, result, featureName, entityName, projectName);
                    }
                }
            }
        }
        
        return result;
    }
    
    private string ExtractFeatureName(string storyDescription)
    {
        // Same logic as FeatureGenerator
        var words = storyDescription.Split(' ');
        var action = words.FirstOrDefault(w => 
            w.Equals("skapa", StringComparison.OrdinalIgnoreCase) ||
            w.Equals("create", StringComparison.OrdinalIgnoreCase) ||
            w.Equals("ta bort", StringComparison.OrdinalIgnoreCase) ||
            w.Equals("delete", StringComparison.OrdinalIgnoreCase) ||
            w.Equals("uppdatera", StringComparison.OrdinalIgnoreCase) ||
            w.Equals("update", StringComparison.OrdinalIgnoreCase) ||
            w.Equals("markera", StringComparison.OrdinalIgnoreCase) ||
            w.Equals("mark", StringComparison.OrdinalIgnoreCase) ||
            w.Equals("se", StringComparison.OrdinalIgnoreCase) ||
            w.Equals("see", StringComparison.OrdinalIgnoreCase) ||
            w.Equals("get", StringComparison.OrdinalIgnoreCase) ||
            w.Equals("sätta", StringComparison.OrdinalIgnoreCase) ||
            w.Equals("set", StringComparison.OrdinalIgnoreCase));
        
        if (string.IsNullOrEmpty(action))
            action = "Get";
        
        var entityMatch = System.Text.RegularExpressions.Regex.Match(
            storyDescription, 
            @"(uppgift|task|todo|item)", 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        
        var entity = entityMatch.Success ? "TodoItem" : "Item";
        
        return $"{Capitalize(action)}{entity}";
    }
    
    private string Capitalize(string str)
    {
        if (string.IsNullOrEmpty(str))
            return str;
        return char.ToUpper(str[0]) + str.Substring(1);
    }
    
    private string InferEntityFromStory(string storyDescription, DomainModel domainModel)
    {
        foreach (var entity in domainModel.Entities)
        {
            if (storyDescription.Contains(entity.Name, StringComparison.OrdinalIgnoreCase) ||
                storyDescription.Contains("uppgift", StringComparison.OrdinalIgnoreCase) && entity.Name == "TodoItem")
            {
                return entity.Name;
            }
        }
        
        return domainModel.Entities.FirstOrDefault()?.Name ?? "Entity";
    }
    
    private bool IsCommand(string storyDescription)
    {
        var lower = storyDescription.ToLower();
        return lower.Contains("skapa") || lower.Contains("create") ||
               lower.Contains("ta bort") || lower.Contains("delete") ||
               lower.Contains("uppdatera") || lower.Contains("update") ||
               lower.Contains("markera") || lower.Contains("mark") ||
               lower.Contains("sätta") || lower.Contains("set");
    }
    
    private void GenerateCommandEndpoint(GenerationContext context, GenerationResult result,
        string featureName, string entityName, string projectName)
    {
        var template = @"using FastEndpoints;
using MediatR;
using {{ project_name }}.Application.Features.{{ entity_name }}s.{{ feature_name }};

namespace {{ project_name }}.Presentation.Endpoints.{{ entity_name }}s;

public class {{ feature_name }}Endpoint : Endpoint<{{ feature_name }}Request, {{ feature_name }}Response>
{
    private readonly IMediator _mediator;
    
    public {{ feature_name }}Endpoint(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    public override void Configure()
    {
        Post(""/api/{{ route_name }}"");
        AllowAnonymous();
    }
    
    public override async Task HandleAsync(
        {{ feature_name }}Request req, 
        CancellationToken ct)
    {
        var command = new {{ feature_name }}Command(
{{ command_parameters}}
        );
        
        var result = await _mediator.Send(command, ct);
        
        await SendAsync(new {{ feature_name }}Response { Id = result }, cancellation: ct);
    }
}

public class {{ feature_name }}Request
{
{{ request_properties}}
}

public class {{ feature_name }}Response
{
    public int Id { get; set; }
}";
        
        var entity = context.DomainModel.Entities.FirstOrDefault(e => e.Name == entityName);
        if (entity == null) return;
        
        var commandParams = new System.Text.StringBuilder();
        var requestProps = new System.Text.StringBuilder();
        
        foreach (var prop in entity.Properties.Where(p => p.Name != "Id"))
        {
            var nullable = prop.IsNullable ? "?" : "";
            var defaultValue = "";
            
            if (prop.Type == "string" && !prop.IsNullable)
            {
                defaultValue = " = string.Empty";
            }
            
            requestProps.AppendLine($"    public {prop.Type}{nullable} {prop.Name} {{ get; set; }}{defaultValue};");
            commandParams.AppendLine($"            req.{prop.Name},");
        }
        
        var routeName = entityName.ToLower() + "s";
        if (featureName.Contains("Create"))
            routeName = routeName.Replace("s", "");
        
        var content = TemplateEngine.Render(template, new
        {
            project_name = projectName,
            entity_name = entityName,
            feature_name = featureName,
            route_name = routeName,
            command_parameters = commandParams.ToString().TrimEnd(','),
            request_properties = requestProps.ToString()
        });
        
        var path = $"src/{projectName}.Presentation/Endpoints/{entityName}s/{featureName}Endpoint.cs";
        WriteFile(path, content, context, result);
    }
    
    private void GenerateQueryEndpoint(GenerationContext context, GenerationResult result,
        string featureName, string entityName, string projectName)
    {
        var template = @"using FastEndpoints;
using MediatR;
using {{ project_name }}.Application.Features.{{ entity_name }}s.{{ feature_name }};

namespace {{ project_name }}.Presentation.Endpoints.{{ entity_name }}s;

public class {{ feature_name }}Endpoint : EndpointWithoutRequest<List<{{ feature_name }}Dto>>
{
    private readonly IMediator _mediator;
    
    public {{ feature_name }}Endpoint(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    public override void Configure()
    {
        Get(""/api/{{ route_name }}"");
        AllowAnonymous();
    }
    
    public override async Task HandleAsync(CancellationToken ct)
    {
        var query = new {{ feature_name }}Query();
        var result = await _mediator.Send(query, ct);
        
        await SendAsync(result, cancellation: ct);
    }
}";
        
        var routeName = entityName.ToLower() + "s";
        
        var content = TemplateEngine.Render(template, new
        {
            project_name = projectName,
            entity_name = entityName,
            feature_name = featureName,
            route_name = routeName
        });
        
        var path = $"src/{projectName}.Presentation/Endpoints/{entityName}s/{featureName}Endpoint.cs";
        WriteFile(path, content, context, result);
    }
}
