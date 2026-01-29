using ProjectGenerator.Core.Models;

namespace ProjectGenerator.Core.Generators;

public class ProjectStructureGenerator : BaseGenerator
{
    public override GenerationResult Generate(GenerationContext context)
    {
        var result = new GenerationResult();
        var projectName = context.ProjectConfig.ProjectName;
        
        // Create main folder structure
        CreateDirectory("src", context, result);
        CreateDirectory($"src/{projectName}.Domain", context, result);
        CreateDirectory($"src/{projectName}.Application", context, result);
        CreateDirectory($"src/{projectName}.Application/Features", context, result);
        CreateDirectory($"src/{projectName}.Application/Common", context, result);
        CreateDirectory($"src/{projectName}.Application/Common/Interfaces", context, result);
        CreateDirectory($"src/{projectName}.Infrastructure", context, result);
        CreateDirectory($"src/{projectName}.Infrastructure/Data", context, result);
        CreateDirectory($"src/{projectName}.Infrastructure/Data/Configurations", context, result);
        CreateDirectory($"src/{projectName}.Infrastructure/Persistence", context, result);
        CreateDirectory($"src/{projectName}.Infrastructure/Services", context, result);
        CreateDirectory($"src/{projectName}.Presentation", context, result);
        CreateDirectory($"src/{projectName}.Presentation/Endpoints", context, result);
        CreateDirectory($"src/{projectName}.Presentation/Properties", context, result);
        
        // Create test folders
        CreateDirectory("tests", context, result);
        CreateDirectory($"tests/{projectName}.Domain.Tests", context, result);
        CreateDirectory($"tests/{projectName}.Application.Tests", context, result);
        CreateDirectory($"tests/{projectName}.Integration.Tests", context, result);
        
        // Create entity folders with Specifications subfolders
        foreach (var entity in context.DomainModel.Entities)
        {
            CreateDirectory($"src/{projectName}.Domain/{entity.Name}", context, result);
            CreateDirectory($"src/{projectName}.Domain/{entity.Name}/Specifications", context, result);
        }
        
        return result;
    }
}
