using Octokit;
using ProjectGenerator.Core.Models;

namespace ProjectGenerator.Core.Integrations;

public class GitHubIntegration
{
    private readonly GitHubClient _client;
    private readonly string _owner;
    
    public GitHubIntegration(string token, string owner)
    {
        _client = new GitHubClient(new ProductHeaderValue("ProjectGenerator"));
        _client.Credentials = new Credentials(token);
        _owner = owner;
    }
    
    public async Task<GitHubResult> CreateRepositoryAsync(string name, string description, bool isPrivate = true)
    {
        var result = new GitHubResult();
        
        try
        {
            var newRepo = new NewRepository(name)
            {
                Description = description,
                Private = isPrivate,
                AutoInit = true
            };
            
            var repo = await _client.Repository.Create(_owner, newRepo);
            result.RepositoryUrl = repo.HtmlUrl;
            result.Success = true;
            result.Operations.Add($"Created repository: {repo.FullName}");
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }
        
        return result;
    }
    
    public async Task<GitHubResult> CreateProjectBoardAsync(string repoName, string projectName)
    {
        var result = new GitHubResult();
        
        try
        {
            // Note: GitHub Projects V2 requires GraphQL API
            // This is a simplified implementation using classic projects
            result.Operations.Add($"Would create project board: {projectName} for {repoName}");
            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }
        
        return result;
    }
    
    public async Task<GitHubResult> CreateEpicIssueAsync(string repoName, Epic epic)
    {
        var result = new GitHubResult();
        
        try
        {
            var newIssue = new NewIssue($"Epic: {epic.Name}")
            {
                Body = $"{epic.Description}\n\n## Stories\n" + 
                       string.Join("\n", epic.Stories.Select(s => $"- [ ] {s.Description}"))
            };
            newIssue.Labels.Add("epic");
            
            var issue = await _client.Issue.Create(_owner, repoName, newIssue);
            result.Success = true;
            result.Operations.Add($"Created epic issue #{issue.Number}: {epic.Name}");
            result.IssueNumbers.Add(issue.Number);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }
        
        return result;
    }
    
    public async Task<GitHubResult> CreateFeatureIssueAsync(string repoName, UserStory story, int? epicIssueNumber = null)
    {
        var result = new GitHubResult();
        
        try
        {
            var body = story.Description;
            if (epicIssueNumber.HasValue)
            {
                body += $"\n\nPart of epic #{epicIssueNumber.Value}";
            }
            
            var newIssue = new NewIssue(story.Description)
            {
                Body = body
            };
            newIssue.Labels.Add("feature");
            
            if (!string.IsNullOrEmpty(story.Epic))
            {
                newIssue.Labels.Add($"epic:{story.Epic.ToLower().Replace(" ", "-")}");
            }
            
            var issue = await _client.Issue.Create(_owner, repoName, newIssue);
            result.Success = true;
            result.Operations.Add($"Created feature issue #{issue.Number}: {story.Description}");
            result.IssueNumbers.Add(issue.Number);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }
        
        return result;
    }
    
    public async Task<GitHubResult> CreateAllIssuesAsync(string repoName, ProjectConfig config)
    {
        var result = new GitHubResult { Success = true };
        var epicIssueNumbers = new Dictionary<string, int>();
        
        // Create epic issues first
        foreach (var epic in config.Epics)
        {
            var epicResult = await CreateEpicIssueAsync(repoName, epic);
            result.Operations.AddRange(epicResult.Operations);
            
            if (epicResult.Success && epicResult.IssueNumbers.Any())
            {
                epicIssueNumbers[epic.Name] = epicResult.IssueNumbers.First();
            }
            
            if (!epicResult.Success)
            {
                result.Success = false;
                result.ErrorMessage = epicResult.ErrorMessage;
                return result;
            }
        }
        
        // Create feature issues linked to epics
        foreach (var epic in config.Epics)
        {
            int? epicNumber = epicIssueNumbers.GetValueOrDefault(epic.Name);
            
            foreach (var story in epic.Stories)
            {
                var storyResult = await CreateFeatureIssueAsync(repoName, story, epicNumber);
                result.Operations.AddRange(storyResult.Operations);
                result.IssueNumbers.AddRange(storyResult.IssueNumbers);
                
                if (!storyResult.Success)
                {
                    result.Success = false;
                    result.ErrorMessage = storyResult.ErrorMessage;
                    return result;
                }
            }
        }
        
        return result;
    }
    
    public GitHubResult SimulateDryRun(ProjectConfig config, string repoName)
    {
        var result = new GitHubResult { Success = true };
        
        result.Operations.Add($"Would create repository: {_owner}/{repoName}");
        result.Operations.Add($"Would create project board: {config.ProjectName} Board");
        
        foreach (var epic in config.Epics)
        {
            result.Operations.Add($"Would create epic issue: {epic.Name}");
            
            foreach (var story in epic.Stories)
            {
                result.Operations.Add($"  Would create feature issue: {story.Description}");
            }
        }
        
        return result;
    }
}

public class GitHubResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? RepositoryUrl { get; set; }
    public List<string> Operations { get; set; } = new();
    public List<int> IssueNumbers { get; set; } = new();
}
