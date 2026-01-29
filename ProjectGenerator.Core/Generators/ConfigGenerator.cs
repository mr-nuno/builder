using ProjectGenerator.Core.Models;

namespace ProjectGenerator.Core.Generators;

public class ConfigGenerator : BaseGenerator
{
    public override GenerationResult Generate(GenerationContext context)
    {
        var result = new GenerationResult();
        var projectName = context.ProjectConfig.ProjectName;
        
        // Generate Directory.Packages.props
        GenerateDirectoryPackagesProps(context, result, projectName);
        
        // Generate solution file
        GenerateSolutionFile(context, result, projectName);
        
        // Generate .csproj files
        GenerateDomainProject(context, result, projectName);
        GenerateApplicationProject(context, result, projectName);
        GenerateInfrastructureProject(context, result, projectName);
        GeneratePresentationProject(context, result, projectName);
        
        // Generate appsettings.json
        GenerateAppSettings(context, result, projectName);
        
        return result;
    }
    
    private void GenerateDirectoryPackagesProps(GenerationContext context, GenerationResult result, string projectName)
    {
        var template = @"<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include=""FastEndpoints"" Version=""5.24.0"" />
    <PackageVersion Include=""MediatR"" Version=""12.2.0"" />
    <PackageVersion Include=""Ardalis.Specification"" Version=""8.0.0"" />
    <PackageVersion Include=""Ardalis.Specification.EntityFrameworkCore"" Version=""8.0.0"" />
    <PackageVersion Include=""FluentValidation"" Version=""11.9.0"" />
    <PackageVersion Include=""FluentValidation.DependencyInjectionExtensions"" Version=""11.9.0"" />
    <PackageVersion Include=""Microsoft.EntityFrameworkCore"" Version=""8.0.0"" />
    <PackageVersion Include=""Microsoft.EntityFrameworkCore.Design"" Version=""8.0.0"" />
    <PackageVersion Include=""Microsoft.EntityFrameworkCore.SqlServer"" Version=""8.0.0"" />
    <PackageVersion Include=""Microsoft.NET.Test.Sdk"" Version=""17.8.0"" />
    <PackageVersion Include=""xunit"" Version=""2.6.2"" />
    <PackageVersion Include=""xunit.runner.visualstudio"" Version=""2.5.4"" />
  </ItemGroup>
</Project>";
        
        WriteFile("Directory.Packages.props", template, context, result);
    }
    
    private void GenerateSolutionFile(GenerationContext context, GenerationResult result, string projectName)
    {
        var template = @"Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
VisualStudioVersion = 17.0.31903.59
MinimumVisualStudioVersion = 10.0.40219.1
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""{{ project_name }}.Domain"", ""src\{{ project_name }}.Domain\{{ project_name }}.Domain.csproj"", ""{DOMAIN_GUID}""
EndProject
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""{{ project_name }}.Application"", ""src\{{ project_name }}.Application\{{ project_name }}.Application.csproj"", ""{APP_GUID}""
EndProject
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""{{ project_name }}.Infrastructure"", ""src\{{ project_name }}.Infrastructure\{{ project_name }}.Infrastructure.csproj"", ""{INFRA_GUID}""
EndProject
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""{{ project_name }}.Presentation"", ""src\{{ project_name }}.Presentation\{{ project_name }}.Presentation.csproj"", ""{PRES_GUID}""
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{DOMAIN_GUID}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{DOMAIN_GUID}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{DOMAIN_GUID}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{DOMAIN_GUID}.Release|Any CPU.Build.0 = Release|Any CPU
		{APP_GUID}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{APP_GUID}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{APP_GUID}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{APP_GUID}.Release|Any CPU.Build.0 = Release|Any CPU
		{INFRA_GUID}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{INFRA_GUID}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{INFRA_GUID}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{INFRA_GUID}.Release|Any CPU.Build.0 = Release|Any CPU
		{PRES_GUID}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{PRES_GUID}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{PRES_GUID}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{PRES_GUID}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
EndGlobal";
        
        var guids = new
        {
            project_name = projectName,
            DOMAIN_GUID = Guid.NewGuid().ToString().ToUpper(),
            APP_GUID = Guid.NewGuid().ToString().ToUpper(),
            INFRA_GUID = Guid.NewGuid().ToString().ToUpper(),
            PRES_GUID = Guid.NewGuid().ToString().ToUpper()
        };
        
        var content = TemplateEngine.Render(template, guids);
        WriteFile($"{projectName}.sln", content, context, result);
    }
    
    private void GenerateDomainProject(GenerationContext context, GenerationResult result, string projectName)
    {
        var template = @"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""Ardalis.Specification"" />
  </ItemGroup>

</Project>";
        
        WriteFile($"src/{projectName}.Domain/{projectName}.Domain.csproj", template, context, result);
    }
    
    private void GenerateApplicationProject(GenerationContext context, GenerationResult result, string projectName)
    {
        var template = @"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include=""..\{{ project_name }}.Domain\{{ project_name }}.Domain.csproj"" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include=""MediatR"" />
    <PackageReference Include=""FluentValidation"" />
    <PackageReference Include=""FluentValidation.DependencyInjectionExtensions"" />
  </ItemGroup>

</Project>";
        
        var content = TemplateEngine.Render(template, new { project_name = projectName });
        WriteFile($"src/{projectName}.Application/{projectName}.Application.csproj", content, context, result);
    }
    
    private void GenerateInfrastructureProject(GenerationContext context, GenerationResult result, string projectName)
    {
        var template = @"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include=""..\{{ project_name }}.Domain\{{ project_name }}.Domain.csproj"" />
    <ProjectReference Include=""..\{{ project_name }}.Application\{{ project_name }}.Application.csproj"" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include=""Microsoft.EntityFrameworkCore"" />
    <PackageReference Include=""Microsoft.EntityFrameworkCore.Design"" />
    <PackageReference Include=""Microsoft.EntityFrameworkCore.SqlServer"" />
    <PackageReference Include=""Ardalis.Specification.EntityFrameworkCore"" />
  </ItemGroup>

</Project>";
        
        var content = TemplateEngine.Render(template, new { project_name = projectName });
        WriteFile($"src/{projectName}.Infrastructure/{projectName}.Infrastructure.csproj", content, context, result);
    }
    
    private void GeneratePresentationProject(GenerationContext context, GenerationResult result, string projectName)
    {
        var template = @"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include=""..\{{ project_name }}.Application\{{ project_name }}.Application.csproj"" />
    <ProjectReference Include=""..\{{ project_name }}.Infrastructure\{{ project_name }}.Infrastructure.csproj"" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include=""FastEndpoints"" />
  </ItemGroup>

</Project>";
        
        var content = TemplateEngine.Render(template, new { project_name = projectName });
        WriteFile($"src/{projectName}.Presentation/{projectName}.Presentation.csproj", content, context, result);
    }
    
    private void GenerateAppSettings(GenerationContext context, GenerationResult result, string projectName)
    {
        var template = @"{
  ""Logging"": {
    ""LogLevel"": {
      ""Default"": ""Information"",
      ""Microsoft.AspNetCore"": ""Warning""
    }
  },
  ""AllowedHosts"": ""*"",
  ""ConnectionStrings"": {
    ""DefaultConnection"": ""Server=localhost;Database={{ db_name }};User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True""
  }
}";
        
        var dbName = $"{projectName}Db";
        var content = TemplateEngine.Render(template, new { db_name = dbName });
        WriteFile($"src/{projectName}.Presentation/appsettings.json", content, context, result);
    }
}
