using ProjectGenerator.Core.Models;

namespace ProjectGenerator.Core.Generators;

public abstract class BaseGenerator : IGenerator
{
    protected readonly TemplateEngine TemplateEngine;
    
    protected BaseGenerator()
    {
        TemplateEngine = new TemplateEngine();
    }
    
    public abstract GenerationResult Generate(GenerationContext context);
    
    protected void WriteFile(string path, string content, GenerationContext context, GenerationResult result)
    {
        var filePreview = new FilePreview
        {
            Path = path,
            Content = content,
            LineCount = content.Split('\n').Length
        };
        
        result.Files.Add(filePreview);
        
        if (!context.IsDryRun)
        {
            var fullPath = Path.Combine(context.OutputDirectory, path);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            File.WriteAllText(fullPath, content);
        }
    }
    
    protected void CreateDirectory(string path, GenerationContext context, GenerationResult result)
    {
        result.Folders.Add(path);
        
        if (!context.IsDryRun)
        {
            var fullPath = Path.Combine(context.OutputDirectory, path);
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }
        }
    }
}
