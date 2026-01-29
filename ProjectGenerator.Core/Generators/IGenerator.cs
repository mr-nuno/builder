using ProjectGenerator.Core.Models;

namespace ProjectGenerator.Core.Generators;

public interface IGenerator
{
    GenerationResult Generate(GenerationContext context);
}
