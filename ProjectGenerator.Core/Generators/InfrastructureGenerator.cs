using ProjectGenerator.Core.Models;

namespace ProjectGenerator.Core.Generators;

public class InfrastructureGenerator : BaseGenerator
{
    public override GenerationResult Generate(GenerationContext context)
    {
        var result = new GenerationResult();
        var projectName = context.ProjectConfig.ProjectName;
        
        // Generate repository interfaces
        GenerateRepositoryInterfaces(context, result, projectName);
        
        // Generate EF repository implementation
        GenerateEfRepository(context, result, projectName);
        
        // Generate DbContext
        GenerateDbContext(context, result, projectName);
        
        // Generate EF configurations
        foreach (var entity in context.DomainModel.Entities)
        {
            GenerateEntityConfiguration(context, result, entity, projectName);
        }
        
        // Generate Program.cs setup
        GenerateProgramCs(context, result, projectName);
        
        // Generate ApplicationAssemblyReference
        GenerateApplicationAssemblyReference(context, result, projectName);
        
        return result;
    }
    
    private void GenerateApplicationAssemblyReference(GenerationContext context, GenerationResult result, string projectName)
    {
        var template = @"namespace {{ project_name }}.Application;

// This class is used for assembly reference in MediatR and FluentValidation registration
public class ApplicationAssemblyReference
{
}";
        
        var content = TemplateEngine.Render(template, new { project_name = projectName });
        WriteFile($"src/{projectName}.Application/ApplicationAssemblyReference.cs", content, context, result);
    }
    
    private void GenerateRepositoryInterfaces(GenerationContext context, GenerationResult result, string projectName)
    {
        var template = @"using Ardalis.Specification;

namespace {{ project_name }}.Application.Common.Interfaces;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> ListAsync(ISpecification<T> spec, CancellationToken cancellationToken = default);
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(T entity, CancellationToken cancellationToken = default);
}

public interface IReadRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> ListAsync(ISpecification<T> spec, CancellationToken cancellationToken = default);
}";
        
        var content = TemplateEngine.Render(template, new { project_name = projectName });
        WriteFile($"src/{projectName}.Application/Common/Interfaces/IRepository.cs", content, context, result);
    }
    
    private void GenerateEfRepository(GenerationContext context, GenerationResult result, string projectName)
    {
        var template = @"using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using {{ project_name }}.Application.Common.Interfaces;
using {{ project_name }}.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace {{ project_name }}.Infrastructure.Persistence;

public class EfRepository<T> : RepositoryBase<T>, IRepository<T>, IReadRepository<T> 
    where T : class
{
    public EfRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
    }
}";
        
        var content = TemplateEngine.Render(template, new { project_name = projectName });
        WriteFile($"src/{projectName}.Infrastructure/Persistence/EfRepository.cs", content, context, result);
    }
    
    private void GenerateDbContext(GenerationContext context, GenerationResult result, string projectName)
    {
        var dbSets = new System.Text.StringBuilder();
        foreach (var entity in context.DomainModel.Entities)
        {
            dbSets.AppendLine($"    public DbSet<{entity.Name}> {entity.Name}s => Set<{entity.Name}>();");
        }
        
        var template = @"using Microsoft.EntityFrameworkCore;
using {{ project_name }}.Domain;
{{ entity_usings }}

namespace {{ project_name }}.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) 
        : base(options)
    {
    }
    
{{ db_sets }}
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}";
        
        var entityUsings = string.Join("\n", context.DomainModel.Entities.Select(e => 
            $"using {projectName}.Domain.{e.Name};"));
        
        var content = TemplateEngine.Render(template, new
        {
            project_name = projectName,
            entity_usings = entityUsings,
            db_sets = dbSets.ToString()
        });
        
        WriteFile($"src/{projectName}.Infrastructure/Data/ApplicationDbContext.cs", content, context, result);
    }
    
    private void GenerateEntityConfiguration(GenerationContext context, GenerationResult result, 
        EntityDefinition entity, string projectName)
    {
        var propertyConfigs = new System.Text.StringBuilder();
        
        foreach (var prop in entity.Properties)
        {
            if (prop.IsRequired)
            {
                propertyConfigs.AppendLine($"            builder.Property(x => x.{prop.Name}).IsRequired();");
            }
            
            if (prop.MaxLength.HasValue && prop.Type == "string")
            {
                propertyConfigs.AppendLine($"            builder.Property(x => x.{prop.Name}).HasMaxLength({prop.MaxLength.Value});");
            }
            
            if (prop.IsUnique)
            {
                propertyConfigs.AppendLine($"            builder.HasIndex(x => x.{prop.Name}).IsUnique();");
            }
        }
        
        var template = @"using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using {{ project_name }}.Domain.{{ entity_name }};

namespace {{ project_name }}.Infrastructure.Data.Configurations;

public class {{ entity_name }}Configuration : IEntityTypeConfiguration<{{ entity_name }}>
{
    public void Configure(EntityTypeBuilder<{{ entity_name }}> builder)
    {
        builder.ToTable(""{{ table_name }}"");
        
        builder.HasKey(x => x.Id);
        
{{ property_configs }}
    }
}";
        
        var content = TemplateEngine.Render(template, new
        {
            project_name = projectName,
            entity_name = entity.Name,
            table_name = entity.Name + "s",
            property_configs = propertyConfigs.ToString()
        });
        
        WriteFile($"src/{projectName}.Infrastructure/Data/Configurations/{entity.Name}Configuration.cs", 
            content, context, result);
    }
    
    private void GenerateProgramCs(GenerationContext context, GenerationResult result, string projectName)
    {
        var template = @"using {{ project_name }}.Application.Common.Interfaces;
using {{ project_name }}.Infrastructure.Data;
using {{ project_name }}.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFastEndpoints();
builder.Services.AddMediatR(cfg => 
    cfg.RegisterServicesFromAssembly(typeof({{ project_name }}.Application.ApplicationAssemblyReference).Assembly));
builder.Services.AddValidatorsFromAssembly(typeof({{ project_name }}.Application.ApplicationAssemblyReference).Assembly);

builder.Services.AddDbContext<ApplicationDbContext>(options => 
    options.UseSqlServer(builder.Configuration.GetConnectionString(""DefaultConnection"")));

builder.Services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
builder.Services.AddScoped(typeof(IReadRepository<>), typeof(EfRepository<>));

var app = builder.Build();

app.UseFastEndpoints();

app.Run();";
        
        var content = TemplateEngine.Render(template, new { project_name = projectName });
        WriteFile($"src/{projectName}.Presentation/Program.cs", content, context, result);
    }
}
