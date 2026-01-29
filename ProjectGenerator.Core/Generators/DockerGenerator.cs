using ProjectGenerator.Core.Models;

namespace ProjectGenerator.Core.Generators;

public class DockerGenerator : BaseGenerator
{
    public override GenerationResult Generate(GenerationContext context)
    {
        var result = new GenerationResult();
        var projectName = context.ProjectConfig.ProjectName;
        
        GenerateDockerfile(context, result, projectName);
        GenerateDockerCompose(context, result, projectName);
        GenerateDockerIgnore(context, result);
        
        return result;
    }
    
    private void GenerateDockerfile(GenerationContext context, GenerationResult result, string projectName)
    {
        var template = @"FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY [""Directory.Packages.props"", "".""]
COPY [""src/{{ project_name }}.Presentation/{{ project_name }}.Presentation.csproj"", ""src/{{ project_name }}.Presentation/""]
COPY [""src/{{ project_name }}.Application/{{ project_name }}.Application.csproj"", ""src/{{ project_name }}.Application/""]
COPY [""src/{{ project_name }}.Domain/{{ project_name }}.Domain.csproj"", ""src/{{ project_name }}.Domain/""]
COPY [""src/{{ project_name }}.Infrastructure/{{ project_name }}.Infrastructure.csproj"", ""src/{{ project_name }}.Infrastructure/""]
RUN dotnet restore ""src/{{ project_name }}.Presentation/{{ project_name }}.Presentation.csproj""
COPY . .
WORKDIR ""/src/src/{{ project_name }}.Presentation""
RUN dotnet build ""{{ project_name }}.Presentation.csproj"" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish ""{{ project_name }}.Presentation.csproj"" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT [""dotnet"", ""{{ project_name }}.Presentation.dll""]";
        
        var content = TemplateEngine.Render(template, new { project_name = projectName });
        WriteFile($"src/{projectName}.Presentation/Dockerfile", content, context, result);
    }
    
    private void GenerateDockerCompose(GenerationContext context, GenerationResult result, string projectName)
    {
        var template = @"version: '3.8'

services:
  api:
    build:
      context: .
      dockerfile: src/{{ project_name }}.Presentation/Dockerfile
    ports:
      - ""8080:80""
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Server=sqlserver;Database={{ db_name }};User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True
    depends_on:
      - sqlserver

  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=YourStrong@Passw0rd
    ports:
      - ""1433:1433""
    volumes:
      - sqlserver_data:/var/opt/mssql

volumes:
  sqlserver_data:";
        
        var dbName = $"{projectName}Db";
        var content = TemplateEngine.Render(template, new 
        { 
            project_name = projectName,
            db_name = dbName
        });
        WriteFile("docker-compose.yml", content, context, result);
    }
    
    private void GenerateDockerIgnore(GenerationContext context, GenerationResult result)
    {
        var content = @"**/.git
**/bin
**/obj
**/.vs
**/.vscode
**/*.user
**/node_modules
**/.env
**/output
**/*.md
!README.md";
        
        WriteFile(".dockerignore", content, context, result);
    }
}
