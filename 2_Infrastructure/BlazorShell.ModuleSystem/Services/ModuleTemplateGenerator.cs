// Infrastructure/Services/ModuleTemplateGenerator.cs
using System.Text;
using Microsoft.Extensions.Logging;

namespace BlazorShell.ModuleSystem.Services
{
    public interface IModuleTemplateGenerator
    {
        Task<ModuleTemplate> GenerateModuleTemplateAsync(ModuleTemplateOptions options);
        Task<bool> CreateModuleProjectAsync(ModuleTemplateOptions options, string outputPath);
        Task<string> GenerateModuleClassAsync(ModuleTemplateOptions options);
        Task<string> GenerateComponentAsync(string componentName, string moduleName);
        Task<string> GenerateServiceAsync(string serviceName, string moduleName);
    }

    public class ModuleTemplateGenerator : IModuleTemplateGenerator
    {
        private readonly ILogger<ModuleTemplateGenerator> _logger;

        public ModuleTemplateGenerator(ILogger<ModuleTemplateGenerator> logger)
        {
            _logger = logger;
        }

        public async Task<ModuleTemplate> GenerateModuleTemplateAsync(ModuleTemplateOptions options)
        {
            var template = new ModuleTemplate
            {
                ModuleName = options.ModuleName,
                Namespace = $"BlazorShell.Modules.{options.ModuleName}",
                Files = new Dictionary<string, string>()
            };

            // Generate main module class
            template.Files[$"{options.ModuleName}Module.cs"] = await GenerateModuleClassAsync(options);

            // Generate project file
            template.Files[$"BlazorShell.Modules.{options.ModuleName}.csproj"] = GenerateProjectFile(options);

            // Generate default component
            if (options.IncludeDefaultComponent)
            {
                template.Files[$"Pages/{options.ModuleName}Page.razor"] =
                    await GenerateComponentAsync($"{options.ModuleName}Page", options.ModuleName);
            }

            // Generate service if requested
            if (options.IncludeService)
            {
                template.Files[$"Services/{options.ModuleName}Service.cs"] =
                    await GenerateServiceAsync($"{options.ModuleName}Service", options.ModuleName);
            }

            // Generate module configuration
            template.Files["module.json"] = GenerateModuleConfig(options);

            return template;
        }

        public async Task<bool> CreateModuleProjectAsync(ModuleTemplateOptions options, string outputPath)
        {
            try
            {
                var template = await GenerateModuleTemplateAsync(options);
                var modulePath = Path.Combine(outputPath, $"BlazorShell.Modules.{options.ModuleName}");

                // Create directory structure
                Directory.CreateDirectory(modulePath);
                Directory.CreateDirectory(Path.Combine(modulePath, "Pages"));
                Directory.CreateDirectory(Path.Combine(modulePath, "Services"));
                Directory.CreateDirectory(Path.Combine(modulePath, "Components"));
                Directory.CreateDirectory(Path.Combine(modulePath, "Models"));

                // Write files
                foreach (var file in template.Files)
                {
                    var filePath = Path.Combine(modulePath, file.Key);
                    var fileDirectory = Path.GetDirectoryName(filePath);

                    if (!string.IsNullOrEmpty(fileDirectory) && !Directory.Exists(fileDirectory))
                    {
                        Directory.CreateDirectory(fileDirectory);
                    }

                    await File.WriteAllTextAsync(filePath, file.Value);
                    _logger.LogInformation("Created file: {File}", filePath);
                }

                _logger.LogInformation("Module project created successfully at {Path}", modulePath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating module project");
                return false;
            }
        }

        public async Task<string> GenerateModuleClassAsync(ModuleTemplateOptions options)
        {
            var sb = new StringBuilder();

            sb.AppendLine("using BlazorShell.Application.Interfaces;");
            sb.AppendLine("using BlazorShell.Domain.Entities;");
            sb.AppendLine("using Microsoft.Extensions.Logging;");
            sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
            sb.AppendLine("using System.Reflection;");
            sb.AppendLine();
            sb.AppendLine($"namespace BlazorShell.Modules.{options.ModuleName}");
            sb.AppendLine("{");
            sb.AppendLine($"    public class {options.ModuleName}Module : IModule{(options.IncludeService ? ", IServiceModule" : "")}");
            sb.AppendLine("    {");
            sb.AppendLine($"        private ILogger<{options.ModuleName}Module>? _logger;");
            sb.AppendLine();
            sb.AppendLine($"        public string Name => \"{options.ModuleName}\";");
            sb.AppendLine($"        public string DisplayName => \"{options.DisplayName ?? options.ModuleName}\";");
            sb.AppendLine($"        public string Description => \"{options.Description ?? $"Module for {options.ModuleName}"}\";");
            sb.AppendLine($"        public string Version => \"1.0.0\";");
            sb.AppendLine($"        public string Author => \"{options.Author ?? "Developer"}\";");
            sb.AppendLine($"        public string Icon => \"{options.Icon ?? "bi bi-puzzle"}\";");
            sb.AppendLine($"        public string Category => \"{options.Category ?? "General"}\";");
            sb.AppendLine($"        public int Order => {options.Order};");
            sb.AppendLine();

            // Initialize method
            sb.AppendLine("        public async Task<bool> InitializeAsync(IServiceProvider serviceProvider)");
            sb.AppendLine("        {");
            sb.AppendLine($"            _logger = serviceProvider.GetService<ILogger<{options.ModuleName}Module>>();");
            sb.AppendLine($"            _logger?.LogInformation(\"Initializing {options.ModuleName} module\");");
            sb.AppendLine("            await Task.CompletedTask;");
            sb.AppendLine("            return true;");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Activate method
            sb.AppendLine("        public async Task<bool> ActivateAsync()");
            sb.AppendLine("        {");
            sb.AppendLine($"            _logger?.LogInformation(\"{options.ModuleName} module activated\");");
            sb.AppendLine("            await Task.CompletedTask;");
            sb.AppendLine("            return true;");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Deactivate method
            sb.AppendLine("        public async Task<bool> DeactivateAsync()");
            sb.AppendLine("        {");
            sb.AppendLine($"            _logger?.LogInformation(\"{options.ModuleName} module deactivated\");");
            sb.AppendLine("            await Task.CompletedTask;");
            sb.AppendLine("            return true;");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Navigation items
            sb.AppendLine("        public IEnumerable<NavigationItem> GetNavigationItems()");
            sb.AppendLine("        {");
            sb.AppendLine("            return new List<NavigationItem>");
            sb.AppendLine("            {");
            sb.AppendLine("                new NavigationItem");
            sb.AppendLine("                {");
            sb.AppendLine($"                    Name = \"{options.ModuleName.ToLower()}\",");
            sb.AppendLine($"                    DisplayName = \"{options.DisplayName ?? options.ModuleName}\",");
            sb.AppendLine($"                    Url = \"/{options.ModuleName.ToLower()}\",");
            sb.AppendLine($"                    Icon = \"{options.Icon ?? "bi bi-puzzle"}\",");
            sb.AppendLine($"                    Order = {options.Order},");
            sb.AppendLine("                    Type = NavigationType.Both,");
            sb.AppendLine("                    IsVisible = true");
            sb.AppendLine("                }");
            sb.AppendLine("            };");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Component types
            sb.AppendLine("        public IEnumerable<Type> GetComponentTypes()");
            sb.AppendLine("        {");
            sb.AppendLine("            var assembly = typeof(" + options.ModuleName + "Module).Assembly;");
            sb.AppendLine("            return assembly.GetTypes()");
            sb.AppendLine("                .Where(t => t.IsSubclassOf(typeof(Microsoft.AspNetCore.Components.ComponentBase)) &&");
            sb.AppendLine("                           !t.IsAbstract &&");
            sb.AppendLine("                           t.GetCustomAttribute<Microsoft.AspNetCore.Components.RouteAttribute>() != null)");
            sb.AppendLine("                .ToList();");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Default settings
            sb.AppendLine("        public Dictionary<string, object> GetDefaultSettings()");
            sb.AppendLine("        {");
            sb.AppendLine("            return new Dictionary<string, object>");
            sb.AppendLine("            {");
            sb.AppendLine("                [\"EnableFeatureX\"] = true,");
            sb.AppendLine("                [\"MaxItems\"] = 100,");
            sb.AppendLine("                [\"DefaultView\"] = \"grid\"");
            sb.AppendLine("            };");
            sb.AppendLine("        }");

            // Service registration if needed
            if (options.IncludeService)
            {
                sb.AppendLine();
                sb.AppendLine("        public void RegisterServices(IServiceCollection services)");
                sb.AppendLine("        {");
                sb.AppendLine($"            services.AddScoped<I{options.ModuleName}Service, {options.ModuleName}Service>();");
                sb.AppendLine($"            _logger?.LogInformation(\"{options.ModuleName} module services registered\");");
                sb.AppendLine("        }");
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            return await Task.FromResult(sb.ToString());
        }

        public async Task<string> GenerateComponentAsync(string componentName, string moduleName)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"@page \"/{moduleName.ToLower()}\"");
            sb.AppendLine("@using BlazorShell.Core.Components");
            sb.AppendLine($"@using BlazorShell.Modules.{moduleName}.Services");
            sb.AppendLine("@inherits ModuleComponentBase");
            sb.AppendLine();
            sb.AppendLine($"<PageTitle>{moduleName}</PageTitle>");
            sb.AppendLine();
            sb.AppendLine("<div class=\"container-fluid\">");
            sb.AppendLine($"    <h1><i class=\"bi bi-puzzle\"></i> {moduleName}</h1>");
            sb.AppendLine();
            sb.AppendLine("    @if (_isLoading)");
            sb.AppendLine("    {");
            sb.AppendLine("        <div class=\"text-center py-5\">");
            sb.AppendLine("            <div class=\"spinner-border text-primary\" role=\"status\"></div>");
            sb.AppendLine("            <p class=\"mt-2\">Loading...</p>");
            sb.AppendLine("        </div>");
            sb.AppendLine("    }");
            sb.AppendLine("    else");
            sb.AppendLine("    {");
            sb.AppendLine("        <div class=\"alert alert-info\">");
            sb.AppendLine($"            <h4>Welcome to {moduleName} Module</h4>");
            sb.AppendLine("            <p>This is your module's default page. Start building your features here!</p>");
            sb.AppendLine("        </div>");
            sb.AppendLine("    }");
            sb.AppendLine("</div>");
            sb.AppendLine();
            sb.AppendLine("@code {");
            sb.AppendLine("    private bool _isLoading = false;");
            sb.AppendLine();
            sb.AppendLine("    protected override async Task OnModuleInitializedAsync()");
            sb.AppendLine("    {");
            sb.AppendLine("        _isLoading = true;");
            sb.AppendLine("        try");
            sb.AppendLine("        {");
            sb.AppendLine("            // Initialize your component here");
            sb.AppendLine("            await Task.Delay(500); // Simulate loading");
            sb.AppendLine("        }");
            sb.AppendLine("        finally");
            sb.AppendLine("        {");
            sb.AppendLine("            _isLoading = false;");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return await Task.FromResult(sb.ToString());
        }

        public async Task<string> GenerateServiceAsync(string serviceName, string moduleName)
        {
            var sb = new StringBuilder();

            sb.AppendLine("using Microsoft.Extensions.Logging;");
            sb.AppendLine();
            sb.AppendLine($"namespace BlazorShell.Modules.{moduleName}.Services");
            sb.AppendLine("{");
            sb.AppendLine($"    public interface I{serviceName}");
            sb.AppendLine("    {");
            sb.AppendLine($"        Task<{moduleName}Data> GetDataAsync();");
            sb.AppendLine($"        Task<bool> SaveDataAsync({moduleName}Data data);");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine($"    public class {serviceName} : I{serviceName}");
            sb.AppendLine("    {");
            sb.AppendLine($"        private readonly ILogger<{serviceName}> _logger;");
            sb.AppendLine();
            sb.AppendLine($"        public {serviceName}(ILogger<{serviceName}> logger)");
            sb.AppendLine("        {");
            sb.AppendLine("            _logger = logger;");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine($"        public async Task<{moduleName}Data> GetDataAsync()");
            sb.AppendLine("        {");
            sb.AppendLine("            _logger.LogInformation(\"Getting data\");");
            sb.AppendLine("            await Task.Delay(100); // Simulate async operation");
            sb.AppendLine();
            sb.AppendLine($"            return new {moduleName}Data");
            sb.AppendLine("            {");
            sb.AppendLine("                Id = Guid.NewGuid().ToString(),");
            sb.AppendLine($"                Name = \"Sample {moduleName} Data\",");
            sb.AppendLine("                CreatedAt = DateTime.UtcNow");
            sb.AppendLine("            };");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine($"        public async Task<bool> SaveDataAsync({moduleName}Data data)");
            sb.AppendLine("        {");
            sb.AppendLine("            _logger.LogInformation(\"Saving data: {Id}\", data.Id);");
            sb.AppendLine("            await Task.Delay(100); // Simulate async operation");
            sb.AppendLine("            return true;");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine($"    public class {moduleName}Data");
            sb.AppendLine("    {");
            sb.AppendLine("        public string Id { get; set; } = string.Empty;");
            sb.AppendLine("        public string Name { get; set; } = string.Empty;");
            sb.AppendLine("        public DateTime CreatedAt { get; set; }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return await Task.FromResult(sb.ToString());
        }

        private string GenerateProjectFile(ModuleTemplateOptions options)
        {
            return $@"<Project Sdk=""Microsoft.NET.Sdk.Razor"">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""Microsoft.AspNetCore.Components.Web"" Version=""8.0.0"" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include=""..\..\BlazorShell.Core\BlazorShell.Core.csproj"" />
  </ItemGroup>

</Project>";
        }

        private string GenerateModuleConfig(ModuleTemplateOptions options)
        {
            return $@"{{
  ""name"": ""{options.ModuleName}"",
  ""displayName"": ""{options.DisplayName ?? options.ModuleName}"",
  ""description"": ""{options.Description ?? $"Module for {options.ModuleName}"}"",
  ""version"": ""1.0.0"",
  ""author"": ""{options.Author ?? "Developer"}"",
  ""category"": ""{options.Category ?? "General"}"",
  ""icon"": ""{options.Icon ?? "bi bi-puzzle"}"",
  ""assemblyName"": ""BlazorShell.Modules.{options.ModuleName}.dll"",
  ""entryType"": ""BlazorShell.Modules.{options.ModuleName}.{options.ModuleName}Module"",
  ""enabled"": true,
  ""loadOrder"": {options.Order},
  ""dependencies"": [],
  ""requiredRole"": null
}}";
        }
    }

    public class ModuleTemplateOptions
    {
        public string ModuleName { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public string? Description { get; set; }
        public string? Author { get; set; }
        public string? Icon { get; set; }
        public string? Category { get; set; }
        public int Order { get; set; } = 100;
        public bool IncludeDefaultComponent { get; set; } = true;
        public bool IncludeService { get; set; } = true;
    }

    public class ModuleTemplate
    {
        public string ModuleName { get; set; } = string.Empty;
        public string Namespace { get; set; } = string.Empty;
        public Dictionary<string, string> Files { get; set; } = new();
    }
}