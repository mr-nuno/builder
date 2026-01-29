# Project Generator

A .NET Core CLI tool that generates complete ASP.NET Core projects from `domain.md` and `requirements.md`, following opinionated architectural patterns:

- **Clean Architecture**: Domain, Application, Infrastructure, and Presentation layers
- **Vertical Slice Architecture**: Features organized by use case
- **FastEndpoints**: Modern API framework (no controllers)
- **MediatR**: CQRS pattern with handlers as inner classes
- **Ardalis Specification**: Reusable EF Core query specifications
- **Docker Support**: Generates Dockerfile and docker-compose.yml
- **GitHub Integration**: Creates repositories, project boards, and issues

## Quick Start

### Using Prebuilt Docker Image (Recommended)

```bash
# Pull the latest image from GitHub Container Registry
docker pull ghcr.io/mr-nuno/builder:latest

# Run with input/output directories
docker run --rm \
  -v $(pwd):/app/input:ro \
  -v $(pwd)/output:/app/output \
  ghcr.io/mr-nuno/builder:latest \
  --input /app/input --output /app/output

# Dry-run to preview what would be generated
docker run --rm \
  -v $(pwd):/app/input:ro \
  ghcr.io/mr-nuno/builder:latest \
  --input /app/input --output /app/output --dry-run
```

### Running Locally

```bash
# Build the project
dotnet build

# Run with dry-run to preview
dotnet run --project ProjectGenerator.Cli -- --input . --output ./generated --dry-run

# Generate a project
dotnet run --project ProjectGenerator.Cli -- --input . --output ./generated
```

### Running with Docker Compose

```bash
# Using docker-compose (builds locally)
docker compose up

# Or build and run manually
docker build -t project-generator .
docker run --rm \
  -v $(pwd):/app/input:ro \
  -v $(pwd)/output:/app/output \
  project-generator \
  --input /app/input --output /app/output
```

## CLI Options

```
--input, -i           Directory containing domain.md and requirements.md (required)
--output, -o          Output directory for generated project (required)
--project-name, -n    Override project name
--dry-run, -d         Preview what would be generated without creating files
--github-token        GitHub Personal Access Token
--github-org          GitHub organization or username
--create-github-repo  Create GitHub repository
--create-github-board Create GitHub project board
```

## Input Files

### domain.md

Define your domain entities with properties and constraints:

```markdown
# Domain Model

## TodoItem
- Title (string): Max 200 tecken, Required
- Description (string): Max 1000 tecken
- IsCompleted (bool): Default false
- Priority (int): Range 1-5, Default 1
- DueDate (datetime?)

## Tag
- Name (string): Unique, Required
- ColorHex (string): Format #RRGGBB
```

**Supported Types:** string, int, bool, datetime, guid, decimal

**Supported Constraints:** Required, Unique, Max N, Min N, Range N-M, Default value, Format

### requirements.md

Define your project requirements with epics and user stories:

```markdown
# ToDo Application

## Epic: Uppgifter
Som användare vill jag kunna hantera mina uppgifter

### Stories
- Som användare vill jag skapa en ny uppgift.
- Som användare vill jag ta bort en uppgift.
- Som användare vill jag uppdatera en uppgift.
- Som användare vill jag markera en uppgift som klar.
- Som användare vill jag se en lista på alla mina uppgifter.
```

## Generated Project Structure

```
GeneratedProject/
├── src/
│   ├── ProjectName.Domain/
│   │   ├── EntityName/
│   │   │   ├── EntityName.cs
│   │   │   └── Specifications/
│   │   │       ├── EntityNameByIdSpec.cs
│   │   │       └── EntityNameAllSpec.cs
│   │   └── BaseEntity.cs
│   ├── ProjectName.Application/
│   │   ├── Features/
│   │   │   └── EntityNames/
│   │   │       └── FeatureName/
│   │   │           ├── FeatureNameCommand.cs (with Handler and Validator as inner classes)
│   │   │           └── FeatureNameDto.cs
│   │   └── Common/
│   │       └── Interfaces/
│   │           └── IRepository.cs
│   ├── ProjectName.Infrastructure/
│   │   ├── Data/
│   │   │   ├── ApplicationDbContext.cs
│   │   │   └── Configurations/
│   │   └── Persistence/
│   │       └── EfRepository.cs
│   └── ProjectName.Presentation/
│       ├── Endpoints/
│       │   └── EntityNames/
│       │       └── FeatureNameEndpoint.cs
│       ├── Program.cs
│       ├── appsettings.json
│       └── Dockerfile
├── Directory.Packages.props
├── docker-compose.yml
└── ProjectName.sln
```

## Architecture

### MediatR Commands with Inner Classes

Commands and queries contain their handlers and validators as inner classes:

```csharp
public record CreateTodoItemCommand(
    string Title,
    string? Description,
    int Priority,
    DateTime? DueDate) : IRequest<int>
{
    public class Handler : IRequestHandler<CreateTodoItemCommand, int>
    {
        private readonly IRepository<TodoItem> _repository;
        
        public Handler(IRepository<TodoItem> repository)
        {
            _repository = repository;
        }
        
        public async Task<int> Handle(CreateTodoItemCommand request, CancellationToken ct)
        {
            var entity = new TodoItem { /* ... */ };
            await _repository.AddAsync(entity, ct);
            return entity.Id;
        }
    }
    
    public class Validator : AbstractValidator<CreateTodoItemCommand>
    {
        public Validator()
        {
            RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Priority).InclusiveBetween(1, 5);
        }
    }
}
```

### FastEndpoints

Endpoints call MediatR commands/queries:

```csharp
public class CreateTodoItemEndpoint : Endpoint<CreateTodoItemRequest, CreateTodoItemResponse>
{
    private readonly IMediator _mediator;
    
    public override void Configure()
    {
        Post("/api/todoitem");
        AllowAnonymous();
    }
    
    public override async Task HandleAsync(CreateTodoItemRequest req, CancellationToken ct)
    {
        var command = new CreateTodoItemCommand(req.Title, req.Description, req.Priority, req.DueDate);
        var result = await _mediator.Send(command, ct);
        await SendAsync(new CreateTodoItemResponse { Id = result }, cancellation: ct);
    }
}
```

## GitHub Integration

Create repositories and issues automatically:

```bash
dotnet run --project ProjectGenerator.Cli -- \
  --input . \
  --output ./generated \
  --github-token $GITHUB_TOKEN \
  --github-org my-organization \
  --create-github-repo \
  --create-github-board
```

This will:
1. Create a GitHub repository
2. Create epic issues for each epic in requirements.md
3. Create feature issues for each user story, linked to their epics

## Dependencies

Generated projects use:
- FastEndpoints
- MediatR
- FluentValidation
- Ardalis.Specification
- Ardalis.Specification.EntityFrameworkCore
- Microsoft.EntityFrameworkCore
- Microsoft.EntityFrameworkCore.SqlServer

## License

Open source - see LICENSE file
