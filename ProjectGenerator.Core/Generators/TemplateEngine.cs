using Scriban;
using Scriban.Runtime;

namespace ProjectGenerator.Core.Generators;

public class TemplateEngine
{
    public string Render(string template, object model)
    {
        var scriptObject = new ScriptObject();
        
        // Add model properties to script object
        var type = model.GetType();
        foreach (var prop in type.GetProperties())
        {
            var value = prop.GetValue(model);
            scriptObject[prop.Name] = value;
        }
        
        var context = new TemplateContext();
        context.PushGlobal(scriptObject);
        
        var parsedTemplate = Template.Parse(template);
        return parsedTemplate.Render(context);
    }
    
    public string RenderFromFile(string templatePath, object model)
    {
        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException($"Template file not found: {templatePath}");
        }
        
        var template = File.ReadAllText(templatePath);
        return Render(template, model);
    }
}
