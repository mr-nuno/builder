using ProjectGenerator.Core.Models;

namespace ProjectGenerator.Core.Generators;

public class FeatureGenerator : BaseGenerator
{
    public override GenerationResult Generate(GenerationContext context)
    {
        var result = new GenerationResult();
        var projectName = context.ProjectConfig.ProjectName;
        
        // Generate features from user stories
        foreach (var epic in context.ProjectConfig.Epics)
        {
            foreach (var story in epic.Stories)
            {
                var featureName = ExtractFeatureName(story.Description);
                var entityName = InferEntityFromStory(story.Description, context.DomainModel);
                
                if (!string.IsNullOrEmpty(entityName))
                {
                    // Determine if it's a command or query
                    if (IsCommand(story.Description))
                    {
                        GenerateCommand(context, result, featureName, entityName, story, projectName);
                    }
                    else
                    {
                        GenerateQuery(context, result, featureName, entityName, story, projectName);
                    }
                }
            }
        }
        
        return result;
    }
    
    private string ExtractFeatureName(string storyDescription)
    {
        // Extract action from story (e.g., "skapa en ny uppgift" -> "CreateTodoItem")
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
        
        // Find entity name
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
        // Try to match story description with entity names
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
    
    private void GenerateCommand(GenerationContext context, GenerationResult result, 
        string featureName, string entityName, UserStory story, string projectName)
    {
        var entity = context.DomainModel.Entities.FirstOrDefault(e => e.Name == entityName);
        if (entity == null) return;
        
        // Generate Command with Handler and Validator as inner classes
        var commandTemplate = @"using MediatR;
using FluentValidation;
using {{ project_name }}.Application.Common.Interfaces;
using {{ project_name }}.Domain.{{ entity_name }};

namespace {{ project_name }}.Application.Features.{{ entity_name }}s.{{ feature_name }};

public record {{ feature_name }}Command(
{{ command_properties }}
) : IRequest<{{ return_type }}>
{
    public class Handler : IRequestHandler<{{ feature_name }}Command, {{ return_type }}>
    {
        private readonly IRepository<{{ entity_name }}> _repository;
        
        public Handler(IRepository<{{ entity_name }}> repository)
        {
            _repository = repository;
        }
        
        public async Task<{{ return_type }}> Handle(
            {{ feature_name }}Command request, 
            CancellationToken cancellationToken)
        {
            var entity = new {{ entity_name }}
            {
{{ entity_initialization}}
            };
            
            await _repository.AddAsync(entity, cancellationToken);
            return entity.Id;
        }
    }
    
    public class Validator : AbstractValidator<{{ feature_name }}Command>
    {
        public Validator()
        {
{{ validation_rules}}
        }
    }
}";
        
        // Build command properties from entity
        var commandProps = new System.Text.StringBuilder();
        var entityInit = new System.Text.StringBuilder();
        var validationRules = new System.Text.StringBuilder();
        
        foreach (var prop in entity.Properties.Where(p => p.Name != "Id"))
        {
            var nullable = prop.IsNullable ? "?" : "";
            commandProps.AppendLine($"    {prop.Type}{nullable} {prop.Name},");
            
            entityInit.AppendLine($"                {prop.Name} = request.{prop.Name},");
            
            if (prop.IsRequired)
            {
                validationRules.AppendLine($"            RuleFor(x => x.{prop.Name})");
                validationRules.AppendLine($"                .NotEmpty();");
            }
            
            if (prop.MaxLength.HasValue)
            {
                validationRules.AppendLine($"            RuleFor(x => x.{prop.Name})");
                validationRules.AppendLine($"                .MaximumLength({prop.MaxLength.Value});");
            }
            
            if (prop.MinValue.HasValue || prop.MaxValue.HasValue)
            {
                validationRules.AppendLine($"            RuleFor(x => x.{prop.Name})");
                if (prop.MinValue.HasValue && prop.MaxValue.HasValue)
                {
                    validationRules.AppendLine($"                .InclusiveBetween({prop.MinValue.Value}, {prop.MaxValue.Value});");
                }
                else if (prop.MinValue.HasValue)
                {
                    validationRules.AppendLine($"                .GreaterThanOrEqualTo({prop.MinValue.Value});");
                }
                else if (prop.MaxValue.HasValue)
                {
                    validationRules.AppendLine($"                .LessThanOrEqualTo({prop.MaxValue.Value});");
                }
            }
        }
        
        var content = TemplateEngine.Render(commandTemplate, new
        {
            project_name = projectName,
            entity_name = entityName,
            feature_name = featureName,
            return_type = "int",
            command_properties = commandProps.ToString().TrimEnd(','),
            entity_initialization = entityInit.ToString().TrimEnd(','),
            validation_rules = validationRules.ToString()
        });
        
        var path = $"src/{projectName}.Application/Features/{entityName}s/{featureName}/{featureName}Command.cs";
        WriteFile(path, content, context, result);
    }
    
    private void GenerateQuery(GenerationContext context, GenerationResult result,
        string featureName, string entityName, UserStory story, string projectName)
    {
        var entity = context.DomainModel.Entities.FirstOrDefault(e => e.Name == entityName);
        if (entity == null) return;
        
        var queryTemplate = @"using MediatR;
using {{ project_name }}.Application.Common.Interfaces;
using {{ project_name }}.Domain.{{ entity_name }};
using {{ project_name }}.Domain.{{ entity_name }}.Specifications;

namespace {{ project_name }}.Application.Features.{{ entity_name }}s.{{ feature_name }};

public record {{ feature_name }}Query : IRequest<List<{{ feature_name }}Dto>>
{
    public class Handler : IRequestHandler<{{ feature_name }}Query, List<{{ feature_name }}Dto>>
    {
        private readonly IReadRepository<{{ entity_name }}> _repository;
        
        public Handler(IReadRepository<{{ entity_name }}> repository)
        {
            _repository = repository;
        }
        
        public async Task<List<{{ feature_name }}Dto>> Handle(
            {{ feature_name }}Query request, 
            CancellationToken cancellationToken)
        {
            var spec = new {{ entity_name }}AllSpec();
            var items = await _repository.ListAsync(spec, cancellationToken);
            
            return items.Select(x => new {{ feature_name }}Dto
            {
{{ dto_mapping}}
            }).ToList();
        }
    }
}

public class {{ feature_name }}Dto
{
{{ dto_properties}}
}";
        
        var dtoProps = new System.Text.StringBuilder();
        var dtoMapping = new System.Text.StringBuilder();
        
        foreach (var prop in entity.Properties)
        {
            var nullable = prop.IsNullable ? "?" : "";
            dtoProps.AppendLine($"    public {prop.Type}{nullable} {prop.Name} {{ get; set; }}");
            
            dtoMapping.AppendLine($"                {prop.Name} = x.{prop.Name},");
        }
        
        var content = TemplateEngine.Render(queryTemplate, new
        {
            project_name = projectName,
            entity_name = entityName,
            feature_name = featureName,
            dto_properties = dtoProps.ToString(),
            dto_mapping = dtoMapping.ToString().TrimEnd(',')
        });
        
        var path = $"src/{projectName}.Application/Features/{entityName}s/{featureName}/{featureName}Query.cs";
        WriteFile(path, content, context, result);
    }
}
