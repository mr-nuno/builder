using System.Text.RegularExpressions;
using ProjectGenerator.Core.Models;

namespace ProjectGenerator.Core.Parsers;

public class RequirementsParser
{
    public ProjectConfig Parse(string content)
    {
        var config = new ProjectConfig();
        var lines = content.Split('\n');
        
        Epic? currentEpic = null;
        bool inStoriesSection = false;
        
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            
            // Project name from first header (# Project Name)
            if (trimmed.StartsWith("# ") && string.IsNullOrEmpty(config.ProjectName))
            {
                config.ProjectName = trimmed.Substring(2).Trim();
                continue;
            }
            
            // Tech stack section
            if (trimmed.Equals("## Tech Stack", StringComparison.OrdinalIgnoreCase))
            {
                // Parse tech stack in next lines
                continue;
            }
            
            if (trimmed.StartsWith("- Framework:"))
            {
                var match = Regex.Match(trimmed, @"Framework:\s*(.+)");
                if (match.Success)
                {
                    var framework = match.Groups[1].Value.Trim();
                    var versionMatch = Regex.Match(framework, @"(\d+\.\d+)");
                    if (versionMatch.Success)
                    {
                        config.FrameworkVersion = versionMatch.Groups[1].Value;
                    }
                }
            }
            
            if (trimmed.StartsWith("- Database:"))
            {
                var match = Regex.Match(trimmed, @"Database:\s*(.+)");
                if (match.Success)
                {
                    config.Database = match.Groups[1].Value.Trim();
                }
            }
            
            // Epic header (## Epic: Name)
            var epicMatch = Regex.Match(trimmed, @"^##\s+Epic:\s*(.+)$", RegexOptions.IgnoreCase);
            if (epicMatch.Success)
            {
                // Save previous epic
                if (currentEpic != null)
                {
                    config.Epics.Add(currentEpic);
                }
                
                currentEpic = new Epic
                {
                    Name = epicMatch.Groups[1].Value.Trim()
                };
                inStoriesSection = false;
                continue;
            }
            
            // Epic description (line after epic header)
            if (currentEpic != null && string.IsNullOrEmpty(currentEpic.Description) && 
                !trimmed.StartsWith("###") && !trimmed.StartsWith("-"))
            {
                if (!string.IsNullOrEmpty(trimmed))
                {
                    currentEpic.Description = trimmed;
                }
                continue;
            }
            
            // Stories section (### Stories)
            if (trimmed.Equals("### Stories", StringComparison.OrdinalIgnoreCase))
            {
                inStoriesSection = true;
                continue;
            }
            
            // User story (- Description)
            if (inStoriesSection && trimmed.StartsWith("-"))
            {
                var storyDescription = trimmed.Substring(1).Trim();
                if (!string.IsNullOrEmpty(storyDescription))
                {
                    var story = new UserStory
                    {
                        Description = storyDescription,
                        Epic = currentEpic?.Name
                    };
                    
                    if (currentEpic != null)
                    {
                        currentEpic.Stories.Add(story);
                    }
                    else
                    {
                        config.Stories.Add(story);
                    }
                }
            }
        }
        
        // Add last epic
        if (currentEpic != null)
        {
            config.Epics.Add(currentEpic);
        }
        
        return config;
    }
}
