using System.CommandLine;
using Spectre.Console;
using ProjectGenerator.Core.Parsers;
using ProjectGenerator.Core.Models;
using ProjectGenerator.Core.Generators;
using ProjectGenerator.Core.Integrations;

var rootCommand = new RootCommand("Project Generator - Generate .NET projects from domain.md and requirements.md");

var inputOption = new Option<string>(
    aliases: new[] { "--input", "-i" },
    description: "Directory containing domain.md and requirements.md")
{
    IsRequired = true
};

var outputOption = new Option<string>(
    aliases: new[] { "--output", "-o" },
    description: "Output directory for generated project")
{
    IsRequired = true
};

var projectNameOption = new Option<string?>(
    aliases: new[] { "--project-name", "-n" },
    description: "Override project name (defaults to name from requirements.md)");

var dryRunOption = new Option<bool>(
    aliases: new[] { "--dry-run", "-d" },
    description: "Preview what would be generated without creating files");

var githubTokenOption = new Option<string?>(
    aliases: new[] { "--github-token" },
    description: "GitHub Personal Access Token");

var githubOrgOption = new Option<string?>(
    aliases: new[] { "--github-org" },
    description: "GitHub organization or username");

var createGithubRepoOption = new Option<bool>(
    aliases: new[] { "--create-github-repo" },
    description: "Create GitHub repository");

var createGithubBoardOption = new Option<bool>(
    aliases: new[] { "--create-github-board" },
    description: "Create GitHub project board");

rootCommand.AddOption(inputOption);
rootCommand.AddOption(outputOption);
rootCommand.AddOption(projectNameOption);
rootCommand.AddOption(dryRunOption);
rootCommand.AddOption(githubTokenOption);
rootCommand.AddOption(githubOrgOption);
rootCommand.AddOption(createGithubRepoOption);
rootCommand.AddOption(createGithubBoardOption);

rootCommand.SetHandler((input, output, projectName, dryRun, githubToken, githubOrg, createRepo, createBoard) =>
{
    AnsiConsole.MarkupLine("[bold blue]Project Generator[/]");
    AnsiConsole.WriteLine();
    
    if (dryRun)
    {
        AnsiConsole.MarkupLine("[yellow]DRY-RUN MODE - No files will be created[/]");
        AnsiConsole.WriteLine();
    }
    
    // Validate input directory
    if (!Directory.Exists(input))
    {
        AnsiConsole.MarkupLine($"[red]Error: Input directory does not exist: {input}[/]");
        return;
    }
    
    var domainPath = Path.Combine(input, "domain.md");
    var requirementsPath = Path.Combine(input, "requirements.md");
    
    if (!File.Exists(domainPath))
    {
        AnsiConsole.MarkupLine($"[red]Error: domain.md not found in {input}[/]");
        return;
    }
    
    if (!File.Exists(requirementsPath))
    {
        AnsiConsole.MarkupLine($"[red]Error: requirements.md not found in {input}[/]");
        return;
    }
    
    // Parse files
    AnsiConsole.Status()
        .Start("Parsing files...", ctx =>
        {
            ctx.Status("Reading domain.md");
            var domainContent = File.ReadAllText(domainPath);
            
            ctx.Status("Reading requirements.md");
            var requirementsContent = File.ReadAllText(requirementsPath);
            
            ctx.Status("Parsing domain model");
            var domainParser = new DomainParser();
            var domainModel = domainParser.Parse(domainContent);
            
            ctx.Status("Parsing requirements");
            var requirementsParser = new RequirementsParser();
            var projectConfig = requirementsParser.Parse(requirementsContent);
            
            // Override project name if provided
            if (!string.IsNullOrEmpty(projectName))
            {
                projectConfig.ProjectName = projectName;
            }
            
            // Display summary
            AnsiConsole.MarkupLine("[green]Files parsed successfully[/]");
            AnsiConsole.WriteLine();
            
            // Show summary table
            var table = new Table();
            table.AddColumn("Item");
            table.AddColumn("Value");
            table.AddRow("Project Name", projectConfig.ProjectName);
            table.AddRow("Entities", domainModel.Entities.Count.ToString());
            table.AddRow("Epics", projectConfig.Epics.Count.ToString());
            table.AddRow("User Stories", projectConfig.Epics.Sum(e => e.Stories.Count).ToString());
            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();
            
            // Create generation context
            var generationContext = new GenerationContext
            {
                IsDryRun = dryRun,
                OutputDirectory = output,
                DomainModel = domainModel,
                ProjectConfig = projectConfig
            };
            
            // Generate project
            ctx.Status("Generating project...");
            var projectGenerator = new ProjectGenerator.Core.Generators.ProjectGenerator();
            var generationResult = projectGenerator.Generate(generationContext);
            
            // Display results
            AnsiConsole.WriteLine();
            
            if (dryRun)
            {
                AnsiConsole.MarkupLine("[yellow]DRY-RUN MODE - No files were created[/]");
                AnsiConsole.WriteLine();
                
                // Show file list
                AnsiConsole.MarkupLine("[bold]Files that would be generated:[/]");
                foreach (var file in generationResult.Files.OrderBy(f => f.Path).Take(20))
                {
                    AnsiConsole.MarkupLine($"  [cyan]{file.Path}[/] [dim]({file.LineCount} lines)[/]");
                }
                
                if (generationResult.Files.Count > 20)
                {
                    AnsiConsole.MarkupLine($"  [dim]... and {generationResult.Files.Count - 20} more files[/]");
                }
                
                AnsiConsole.WriteLine();
            }
            else
            {
                AnsiConsole.MarkupLine($"[green]Project generated successfully to: {output}[/]");
            }
            
            // Show summary
            var summaryTable = new Table();
            summaryTable.AddColumn("Statistic");
            summaryTable.AddColumn("Value");
            summaryTable.AddRow("Total Files", generationResult.Files.Count.ToString());
            summaryTable.AddRow("Total Folders", generationResult.Folders.Distinct().Count().ToString());
            summaryTable.AddRow("Total Lines", generationResult.Files.Sum(f => f.LineCount).ToString());
            AnsiConsole.Write(summaryTable);
            
            // GitHub integration
            if (createRepo || createBoard)
            {
                AnsiConsole.WriteLine();
                
                if (string.IsNullOrEmpty(githubToken) || string.IsNullOrEmpty(githubOrg))
                {
                    AnsiConsole.MarkupLine("[red]Error: GitHub token and organization are required for GitHub operations[/]");
                    return;
                }
                
                var repoName = projectConfig.ProjectName.Replace(" ", "-").ToLower();
                
                if (dryRun)
                {
                    AnsiConsole.MarkupLine("[bold blue]GitHub Operations (Dry Run)[/]");
                    var github = new GitHubIntegration(githubToken, githubOrg);
                    var gitHubDryRunResult = github.SimulateDryRun(projectConfig, repoName);
                    
                    foreach (var op in gitHubDryRunResult.Operations)
                    {
                        AnsiConsole.MarkupLine($"[dim]  {op}[/]");
                    }
                }
                else
                {
                    var github = new GitHubIntegration(githubToken, githubOrg);
                    
                    if (createRepo)
                    {
                        ctx.Status("Creating GitHub repository...");
                        var repoResult = github.CreateRepositoryAsync(repoName, $"Generated project: {projectConfig.ProjectName}").GetAwaiter().GetResult();
                        
                        if (repoResult.Success)
                        {
                            AnsiConsole.MarkupLine($"[green]Repository created: {repoResult.RepositoryUrl}[/]");
                            
                            // Create issues for epics and stories
                            ctx.Status("Creating GitHub issues...");
                            var issuesResult = github.CreateAllIssuesAsync(repoName, projectConfig).GetAwaiter().GetResult();
                            
                            if (issuesResult.Success)
                            {
                                AnsiConsole.MarkupLine($"[green]Created {issuesResult.IssueNumbers.Count} issues[/]");
                            }
                            else
                            {
                                AnsiConsole.MarkupLine($"[red]Error creating issues: {issuesResult.ErrorMessage}[/]");
                            }
                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"[red]Error creating repository: {repoResult.ErrorMessage}[/]");
                        }
                    }
                    
                    if (createBoard)
                    {
                        ctx.Status("Creating GitHub project board...");
                        var boardResult = github.CreateProjectBoardAsync(repoName, $"{projectConfig.ProjectName} Board").GetAwaiter().GetResult();
                        
                        if (boardResult.Success)
                        {
                            AnsiConsole.MarkupLine("[green]Project board created[/]");
                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"[red]Error creating project board: {boardResult.ErrorMessage}[/]");
                        }
                    }
                }
            }
        });
},
inputOption, outputOption, projectNameOption, dryRunOption, githubTokenOption, githubOrgOption, createGithubRepoOption, createGithubBoardOption);

await rootCommand.InvokeAsync(args);
