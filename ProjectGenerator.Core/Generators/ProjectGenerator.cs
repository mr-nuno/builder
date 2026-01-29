using ProjectGenerator.Core.Models;

namespace ProjectGenerator.Core.Generators;

public class ProjectGenerator
{
    private readonly List<IGenerator> _generators;
    
    public ProjectGenerator()
    {
        _generators = new List<IGenerator>
        {
            new ProjectStructureGenerator(),
            new ConfigGenerator(),
            new DomainGenerator(),
            new InfrastructureGenerator(),
            new FeatureGenerator(),
            new EndpointGenerator(),
            new DockerGenerator()
        };
    }
    
    public GenerationResult Generate(GenerationContext context)
    {
        var overallResult = new GenerationResult();
        
        foreach (var generator in _generators)
        {
            var result = generator.Generate(context);
            
            // Merge results
            overallResult.Files.AddRange(result.Files);
            overallResult.Folders.AddRange(result.Folders);
            overallResult.Statistics = result.Statistics;
            overallResult.GitHubOperations.AddRange(result.GitHubOperations);
        }
        
        // Calculate statistics
        overallResult.Statistics["TotalFiles"] = overallResult.Files.Count;
        overallResult.Statistics["TotalFolders"] = overallResult.Folders.Distinct().Count();
        overallResult.Statistics["TotalLines"] = overallResult.Files.Sum(f => f.LineCount);
        
        return overallResult;
    }
}
