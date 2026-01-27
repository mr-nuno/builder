using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using CliWrap;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Scriban;
using Spectre.Console;

public class Program
{
    public static async Task Main(string[] args)
    {
        // ==================================================================================
        // 1. CONFIGURATION & SETUP
        // ==================================================================================
        
        var configBuilder = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddUserSecrets<Program>()
            .AddEnvironmentVariables();
        
        IConfiguration config = configBuilder.Build();
        var apiKey = config["GEMINI_KEY"];
        var ghToken = config["GH_TOKEN"];

        if (!string.IsNullOrEmpty(ghToken))
        {
            Environment.SetEnvironmentVariable("GITHUB_TOKEN", ghToken);
        }
        
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            AnsiConsole.MarkupLine("[bold red]❌ GEMINI_KEY saknas.[/]");
            AnsiConsole.MarkupLine("[grey]Sätt den via Environment Variables eller User Secrets.[/]");
            return;
        }

        AnsiConsole.Write(new FigletText(".NET 10 Builder").Color(Color.Cyan1));

        // ==================================================================================
        // 2. INPUT HANDLING (Local vs Docker)
        // ==================================================================================

        var defaultFile = "requirements.md";
        var inputPath = args.Length > 0 ? args[0] : defaultFile;

        // Docker-fix: Om vi står i roten men filen är i /data (mount)
        if (!File.Exists(inputPath) && File.Exists("/data/" + inputPath)) 
            inputPath = "/data/" + inputPath;

        var fullInputPath = Path.GetFullPath(inputPath);

        if (!File.Exists(fullInputPath))
        {
            AnsiConsole.MarkupLine($"[bold red]❌ Hittade inte filen: {fullInputPath}[/]");
            return;
        }

        // Byt arbetskatalog till där filen ligger (så projektet skapas där)
        var targetDir = Path.GetDirectoryName(fullInputPath) ?? Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(targetDir);
        
        AnsiConsole.MarkupLine($"[blue]📂 Working Directory: {targetDir}[/]");

        // ==================================================================================
        // 3. AI ANALYS
        // ==================================================================================

        // Läs Requirements
        var reqText = await File.ReadAllTextAsync(fullInputPath);
        
        // Läs Domain Model (om den finns)
        var domainPath = Path.Combine(targetDir, "domain.md");
        var domainText = "";
        if (File.Exists(domainPath))
        {
            AnsiConsole.MarkupLine($"[green]🟢 Inkluderar domänmodell: {Path.GetFileName(domainPath)}[/]");
            domainText = await File.ReadAllTextAsync(domainPath);
        }

        var combinedContext = $"# REQUIREMENTS\n{reqText}\n\n# DOMAIN MODEL (SOURCE OF TRUTH)\n{domainText}";

        var promptContent = await CreatePrompt(Path.Combine(targetDir, "system_prompt.txt"));        
        
        AnsiConsole.MarkupLine("[yellow]🤖 Pratar med Gemini...[/]");
        
        var rawJson = await CallGemini(apiKey, promptContent, combinedContext);
        
        // Städa JSON (Ta bort ```json block)
        var cleanJson = CleanJson(rawJson);

        Blueprint? blueprint = null;
        try 
        {
            var settings = new JsonSerializerSettings 
            { 
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                MissingMemberHandling = MissingMemberHandling.Ignore 
            };
            blueprint = JsonConvert.DeserializeObject<Blueprint>(cleanJson, settings);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]❌ JSON Parsing misslyckades: {ex.Message}[/]");
            // Debug: Visa vad AI skickade om det failar
            AnsiConsole.MarkupLine("[grey]Raw response:[/]");
            Console.WriteLine(rawJson);
            return;
        }

        if (blueprint == null || string.IsNullOrEmpty(blueprint.ProjectName))
        {
            AnsiConsole.MarkupLine("[red]❌ Ogiltig Blueprint från AI.[/]");
            return;
        }

        // Validate project name (sanitize for file system)
        var invalidChars = Path.GetInvalidFileNameChars();
        if (blueprint.ProjectName.IndexOfAny(invalidChars) >= 0)
        {
            AnsiConsole.MarkupLine($"[red]❌ Projektnamnet innehåller ogiltiga tecken: {blueprint.ProjectName}[/]");
            return;
        }

        // Validate entities
        if (blueprint.Entities == null || blueprint.Entities.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]❌ Blueprint saknar entiteter. Minst en entitet krävs.[/]");
            return;
        }

        // Validate entity names
        foreach (var entity in blueprint.Entities)
        {
            if (string.IsNullOrWhiteSpace(entity.Name))
            {
                AnsiConsole.MarkupLine("[red]❌ En entitet saknar namn.[/]");
                return;
            }
            if (entity.Name.IndexOfAny(invalidChars) >= 0)
            {
                AnsiConsole.MarkupLine($"[red]❌ Entitetsnamn innehåller ogiltiga tecken: {entity.Name}[/]");
                return;
            }
        }

        AnsiConsole.MarkupLine($"[green]✅ Arkitektur klar: {blueprint.ProjectName}[/]");

        // ==================================================================================
        // 4. GITHUB SYNC
        // ==================================================================================
        
        if (!Directory.Exists(".git"))
        {
            AnsiConsole.MarkupLine("[yellow]🐙 Initierar Git...[/]");
            await Run("git", "init");

            // Försök bara om GH_TOKEN finns eller om vi är inloggade lokalt
            try 
            {
                // Skapa repo
                AnsiConsole.MarkupLine("[yellow]📦 Skapar GitHub repository...[/]");
                await Run("gh", $"repo create {blueprint.ProjectName} --private --source=. --push");
                
                // Labels
                await Run("gh", "label create epic --color 800080 --force");
                await Run("gh", "label create story --color 0E8A16 --force");

                foreach (var epic in blueprint.Epics ?? new())
                {
                    // Skapa Epic Issue
                    var epicUrl = await RunOutput("gh", $"issue create -t \"Epic: {epic.Title}\" -b \"Parent Epic\" -l epic");
                    
                    // Parse epic ID from URL (format: https://github.com/owner/repo/issues/123)
                    var epicId = ExtractIssueId(epicUrl);
                    if (string.IsNullOrEmpty(epicId))
                    {
                        AnsiConsole.MarkupLine($"[yellow]⚠️ Kunde inte extrahera Epic ID från: {epicUrl}[/]");
                        continue;
                    }
                    
                    var tasks = new StringBuilder("### User Stories\n");
                    
                    foreach (var story in epic.Stories ?? new())
                    {
                        var safeTitle = story.Length > 50 ? story[..47] + "..." : story;
                        // Skapa Story Issue
                        var storyUrl = await RunOutput("gh", $"issue create -t \"{safeTitle}\" -b \"{story}\" -l story");
                        var storyId = ExtractIssueId(storyUrl);
                        
                        if (!string.IsNullOrEmpty(storyId))
                        {
                            // Lägg till i Epicens task list
                            tasks.AppendLine($"- [ ] #{storyId} {safeTitle}");
                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"[yellow]⚠️ Kunde inte extrahera Story ID från: {storyUrl}[/]");
                        }
                    }
                    // Uppdatera Epic med länkar
                    await Run("gh", $"issue edit {epicId} -b \"{tasks}\"");
                }
            } 
            catch (Exception ex)
            { 
                AnsiConsole.MarkupLine($"[grey]⚠️ GitHub sync misslyckades: {ex.Message}[/]");
                AnsiConsole.MarkupLine("[grey]Kontrollera att GitHub CLI ('gh') är installerat och att GH_TOKEN är korrekt.[/]"); 
            }
        }

        // ==================================================================================
        // 5. SCAFFOLDING (Mappar & Projekt)
        // ==================================================================================
        
        var slnName = blueprint.ProjectName;
        if(!Directory.Exists(slnName)) Directory.CreateDirectory(slnName);
        Environment.CurrentDirectory = Path.Combine(Environment.CurrentDirectory, slnName);

        // Props filer
        await Render("Directory.Packages.props.j2", "Directory.Packages.props", blueprint);
        await Render("Directory.Build.props.j2", "Directory.Build.props", blueprint);

        // Skapa Solution
        await Run("dotnet", $"new sln -n {slnName}");

        // Skapa projektmappar och generera .csproj-filer från templates
        AnsiConsole.MarkupLine("[yellow]📁 Skapar projektstruktur...[/]");
        
        var projectModel = new { @namespace = slnName };
        
        // Domain
        Directory.CreateDirectory($"src/{slnName}.Domain");
        await Render("Domain/Domain.csproj.j2", $"src/{slnName}.Domain/{slnName}.Domain.csproj", projectModel);
        await Run("dotnet", $"sln add src/{slnName}.Domain/{slnName}.Domain.csproj");
        
        // Application
        Directory.CreateDirectory($"src/{slnName}.Application");
        await Render("Application/Application.csproj.j2", $"src/{slnName}.Application/{slnName}.Application.csproj", projectModel);
        await Run("dotnet", $"sln add src/{slnName}.Application/{slnName}.Application.csproj");
        
        // Infrastructure
        Directory.CreateDirectory($"src/{slnName}.Infrastructure");
        await Render("Infrastructure/Infrastructure.csproj.j2", $"src/{slnName}.Infrastructure/{slnName}.Infrastructure.csproj", projectModel);
        await Run("dotnet", $"sln add src/{slnName}.Infrastructure/{slnName}.Infrastructure.csproj");
        
        // Api
        Directory.CreateDirectory($"src/{slnName}.Api");
        await Render("Api/Api.csproj.j2", $"src/{slnName}.Api/{slnName}.Api.csproj", projectModel);
        await Run("dotnet", $"sln add src/{slnName}.Api/{slnName}.Api.csproj");
        
        // Testprojekt
        var testProj = $"{slnName}.IntegrationTests";
        Directory.CreateDirectory($"tests/{testProj}");
        await Render("Tests/IntegrationTests.csproj.j2", $"tests/{testProj}/{testProj}.csproj", projectModel);
        await Run("dotnet", $"sln add tests/{testProj}/{testProj}.csproj");

        // ==================================================================================
        // 6. KODGENERERING (Scriban)
        // ==================================================================================

        // Sökvägar
        var dPath = $"src/{slnName}.Domain";
        var aPath = $"src/{slnName}.Application";
        var iPath = $"src/{slnName}.Infrastructure";
        var apiPath = $"src/{slnName}.Api";

        // --- COMMON / INFRASTRUCTURE ---
        await Render("Domain/Common/IAuditableEntity.j2", $"{dPath}/Common/IAuditableEntity.cs", new { @namespace = slnName });
        await Render("Application/Common/Interfaces/IDateTimeProvider.j2", $"{aPath}/Common/Interfaces/IDateTimeProvider.cs", new { @namespace = slnName });
        await Render("Application/Common/Interfaces/IUserSession.j2", $"{aPath}/Common/Interfaces/IUserSession.cs", new { @namespace = slnName });
        
        await Render("Infrastructure/Services/DateTimeService.j2", $"{iPath}/Services/DateTimeService.cs", new { @namespace = slnName });
        await Render("Infrastructure/Services/UserSession.j2", $"{iPath}/Services/UserSession.cs", new { @namespace = slnName });
        await Render("Infrastructure/Data/Interceptors/AuditableEntityInterceptor.j2", $"{iPath}/Data/Interceptors/AuditableEntityInterceptor.cs", new { @namespace = slnName });
        await Render("Infrastructure/DependencyInjection.j2", $"{iPath}/DependencyInjection.cs", new { @namespace = slnName });
        await Render("Infrastructure/Data/AppDbContext.j2", $"{iPath}/Data/AppDbContext.cs", new { @namespace = slnName, entity_list = blueprint.Entities });

        await Render("Api/Program.j2", $"{apiPath}/Program.cs", new { projectName = slnName });
        await Render("Api/appsettings.json.j2", $"{apiPath}/appsettings.json", new { projectName = slnName });
        
        // --- DOCKER ---
        await Render("Api/Dockerfile.j2", $"{apiPath}/Dockerfile", new { @namespace = slnName });
        await Render("docker-compose.yml.j2", "docker-compose.yml", new { @namespace = slnName });

        // --- FEATURES (Per Entity) ---
        foreach (var entity in blueprint.Entities)
        {
            var model = new { @namespace = slnName, entity = entity };

            // Domain
            await Render("Domain/Entity.j2", $"{dPath}/Entities/{entity.Name}.cs", model);

            // Application - Command (Nested: Command + Handler + Validator)
            var fPath = $"{aPath}/Features/{entity.Name}s";
            await Render("Application/Command.j2", $"{fPath}/Commands/Create{entity.Name}.cs", model);
            
            // Application - Query (Nested: Query + Handler + DTO)
            await Render("Application/Query.j2", $"{fPath}/Queries/Get{entity.Name}ById.cs", model);

            // API - Endpoints
            await Render("Api/Endpoint.j2", $"{apiPath}/Features/{entity.Name}s/Create{entity.Name}Endpoint.cs", model);
            await Render("Api/QueryEndpoint.j2", $"{apiPath}/Features/{entity.Name}s/Get{entity.Name}ByIdEndpoint.cs", model);

            // Tests
            await Render("Tests/Integration/IntegrationTest.j2", $"tests/{testProj}/Features/{entity.Name}s/Create{entity.Name}Tests.cs", model);
        }
        
        // Test Infrastructure
        await Render("Tests/Integration/AppFixture.j2", $"tests/{testProj}/AppFixture.cs", new { @namespace = slnName });

        // ==================================================================================
        // 7. DATABASE MIGRATIONS
        // ==================================================================================
        
        AnsiConsole.MarkupLine("[yellow]📦 Förbereder databasen (Migrations)...[/]");
        await Run("dotnet", "restore"); // Krävs för verktygen

        try 
        {
            // Skapa InitialCreate migration
            await Run("dotnet", $"ef migrations add InitialCreate -p src/{slnName}.Infrastructure -s src/{slnName}.Api");
            AnsiConsole.MarkupLine("[green]✅ Migration 'InitialCreate' skapad.[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine("[red]⚠️ Kunde inte skapa migrationer (dotnet-ef kanske saknas i miljön).[/]");
            AnsiConsole.MarkupLine($"[grey]Fel: {ex.Message}[/]");
        }

        AnsiConsole.MarkupLine("[bold green]🚀 KLART! Systemet är byggt.[/]");
        AnsiConsole.MarkupLine($"[grey]Kör 'docker compose up --build' för att starta.[/]");
    }

    // ==================================================================================
    // HELPERS
    // ==================================================================================

    static async Task<string> CreatePrompt(string path) 
    {
        // --- NY LOGIK: Läs System Prompt från input-mappen ---
        if (File.Exists(path))
        {
            AnsiConsole.MarkupLine($"[green]🧠 Använder anpassad System Prompt: {path}[/]");
            return await File.ReadAllTextAsync(path);
        }
 
        AnsiConsole.MarkupLine($"[bold red]❌ Ingen 'system_prompt.txt' hittades i {path}.[/]");
        AnsiConsole.MarkupLine("[yellow]System Prompt är obligatorisk för att generera blueprint.[/]");
        throw new FileNotFoundException($"System prompt file not found: {path}");
    }
    
    static async Task Render(string tplName, string outPath, object model)
    {
        var tplContent = await ResourceReader.ReadAsync($"templates/{tplName}");
        var result = await Template.Parse(tplContent).RenderAsync(model, member => member.Name);
        
        var dir = Path.GetDirectoryName(outPath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        
        await File.WriteAllTextAsync(outPath, result);
        AnsiConsole.MarkupLine($"   📄 [grey]{Path.GetFileName(outPath)}[/]");
    }

    static async Task Run(string exe, string args) 
        => await Cli.Wrap(exe).WithArguments(args).WithValidation(CommandResultValidation.None).ExecuteAsync();

    static async Task<string> RunOutput(string exe, string args)
    {
        var sb = new StringBuilder();
        await Cli.Wrap(exe).WithArguments(args).WithStandardOutputPipe(PipeTarget.ToStringBuilder(sb)).WithValidation(CommandResultValidation.None).ExecuteAsync();
        return sb.ToString().Trim();
    }

    static string CleanJson(string json)
    {
        if (string.IsNullOrEmpty(json)) return "{}";
        // Regex för att hitta content mellan ```json och ```
        var match = Regex.Match(json, @"```json\s*(.*?)\s*```", RegexOptions.Singleline);
        if (match.Success) return match.Groups[1].Value;
        
        // Fallback: Ta bort bara ```
        return json.Replace("```json", "").Replace("```", "").Trim();
    }

    static string? ExtractIssueId(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        
        // Try to extract ID from URL format: https://github.com/owner/repo/issues/123
        // or just the number if it's already extracted
        var parts = url.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length > 0)
        {
            var lastPart = parts[^1];
            // If last part is a number, return it
            if (int.TryParse(lastPart, out _))
            {
                return lastPart;
            }
        }
        
        // Fallback: try regex to find issue number
        var match = Regex.Match(url, @"/issues/(\d+)");
        if (match.Success && match.Groups.Count > 1)
        {
            return match.Groups[1].Value;
        }
        
        return null;
    }

    static async Task ListAvailableModels(string key)
    {
        using var client = new HttpClient();
        var url = $"https://generativelanguage.googleapis.com/v1beta/models?key={key}";
        try 
        {
            var json = await client.GetStringAsync(url);
            AnsiConsole.MarkupLine("[yellow]Tillgängliga modeller:[/]");
            Console.WriteLine(json); // Skriver ut rå JSON
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Kunde inte lista modeller: {ex.Message}[/]");
        }
    }
    
    static async Task<string> CallGemini(string key, string sys, string user)
    {
        using var client = new HttpClient();
        // Använd specifik version för stabilitet
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={key}";

        var body = new
        {
            system_instruction = new { parts = new[] { new { text = sys } } },
            contents = new[] { new { parts = new[] { new { text = user } } } },
            generation_config = new { response_mime_type = "application/json" }
        };

        var jsonContent = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
        var response = await client.PostAsync(url, jsonContent);
        var responseString = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            // Kasta fel så vi ser det i konsolen
            throw new Exception($"Gemini Error {response.StatusCode}: {responseString}");
        }

        var json = JObject.Parse(responseString);
        return json["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString() ?? "{}";
    }

    public static class ResourceReader
    {
        public static async Task<string> ReadAsync(string path)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var assemblyName = assembly.GetName().Name;
        
            // Normalisera sökvägen (templates/Domain/...) -> (templates.Domain...)
            var relativePath = path.Replace('/', '.').Replace('\\', '.');
            var expectedName = $"{assemblyName}.{relativePath}";

            // Försök hitta resursen (Case Insensitive för säkerhets skull)
            var resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(r => r.Equals(expectedName, StringComparison.InvariantCultureIgnoreCase));

            if (resourceName != null)
            {
                using var stream = assembly.GetManifestResourceStream(resourceName);
                using var reader = new StreamReader(stream!);
                return await reader.ReadToEndAsync();
            }

            // --- FELSÖKNINGS-LÄGE ---
            AnsiConsole.MarkupLine($"[bold red]❌ CRITICAL ERROR: Kunde inte hitta resursen: {expectedName}[/]");
            
            PrintAllEmbeddedResources(assembly);

            throw new FileNotFoundException($"Missing embedded resource: {path}");
        }
    }

    static void PrintAllEmbeddedResources(Assembly assembly)
    {
        AnsiConsole.MarkupLine("[yellow]🔎 Lista över ALLA inbakade resurser i .exe-filen:[/]");
        
        var allResources = assembly.GetManifestResourceNames();
        if (allResources.Length == 0)
        {
            AnsiConsole.MarkupLine("[red]   (Listan är tom! Inga filer bakades in vid kompilering.)[/]");
            AnsiConsole.MarkupLine("[grey]   Kolla din .csproj fil och att du använder '/' (forward slash).[/]");
        }
        else
        {
            foreach (var res in allResources)
            {
                Console.WriteLine($"   - {res}");
            }
        }
    }
    
    // DTOs
    record Blueprint(string ProjectName, List<Epic> Epics, List<Entity> Entities);
    record Epic(string Title, List<string> Stories);
    record Entity(string Name, List<Property> Properties);
    record Property(string Name, string Type, List<string> Rules);
}
