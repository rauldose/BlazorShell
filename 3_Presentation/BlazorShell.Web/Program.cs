using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Authorization;
using BlazorShell.Infrastructure.Data;
using BlazorShell.Infrastructure.Services;
using BlazorShell.Infrastructure.Security;
using BlazorShell.Components;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using System.Reflection;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.ResponseCompression;
using Module = Autofac.Module;
using Autofac.Core;
using BlazorShell.Components.Account;
using IdentityRevalidatingAuthenticationStateProvider = BlazorShell.Components.Account.IdentityRevalidatingAuthenticationStateProvider;
using BlazorShell.Application.Interfaces;
using BlazorShell.Application.Services;
using BlazorShell.Domain.Entities;
using BlazorShell.ModuleSystem.Services;
using BlazorShell.Web.Components;
using BlazorShell.ModuleSystem;
using BlazorShell.Infrastructure;
using BlazorShell.Application;

var builder = WebApplication.CreateBuilder(args);

// Store service collection for later use
var serviceCollection = builder.Services;

// Configure Autofac as DI container for plugin support
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
{
    // Register core services - no constructor parameters for Autofac modules
    containerBuilder.RegisterModule(new CoreServicesModule());

    // Register infrastructure services
    //containerBuilder.RegisterModule(new InfrastructureModule());

    // Register ModuleServiceProvider here with access to serviceCollection
    containerBuilder.Register(c =>
    {
        var rootProvider = c.Resolve<IServiceProvider>();
        var logger = c.Resolve<ILogger<ModuleServiceProvider>>();
        return new ModuleServiceProvider(rootProvider, serviceCollection, logger);
    })
    .As<IModuleServiceProvider>()
    .SingleInstance();
});

// Add framework services - Updated for .NET 8 Blazor Web App
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add Razor Pages for Identity pages
builder.Services.AddRazorPages();


builder.Services.AddModuleSystem();
// Configure SignalR for InteractiveServer mode
builder.Services.AddSignalR(options =>
{
    // Configure SignalR for production
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
});

// Configure response compression for performance
builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/octet-stream" });
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Configure Entity Framework with connection resiliency
//builder.Services.AddDbContext<ApplicationDbContext>(options =>
//{
//    options.UseSqlServer(
//        builder.Configuration.GetConnectionString("DefaultConnection"),
//        sqlOptions =>
//        {
//            sqlOptions.EnableRetryOnFailure(
//                maxRetryCount: 5,
//                maxRetryDelay: TimeSpan.FromSeconds(30),
//                errorNumbersToAdd: null);
//            sqlOptions.CommandTimeout(30);
//        });

//    if (builder.Environment.IsDevelopment())
//    {
//        options.EnableSensitiveDataLogging();
//        options.EnableDetailedErrors();
//    }
//});

// Configure Identity with enhanced security
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequiredUniqueChars = 6;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = false; // Set to true in production
})
.AddRoles<ApplicationRole>()
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Register dynamic route service
builder.Services.AddSingleton<IDynamicRouteService, DynamicRouteService>();

// Add cascading authentication state (new .NET 8 pattern)
builder.Services.AddCascadingAuthenticationState();

// Configure authentication with security stamp validation
builder.Services.AddScoped<IHostEnvironmentAuthenticationStateProvider>(sp =>
    sp.GetRequiredService<IdentityRevalidatingAuthenticationStateProvider>());
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<IdentityRevalidatingAuthenticationStateProvider>());
builder.Services.AddScoped<IdentityRevalidatingAuthenticationStateProvider>();

// Configure authorization with policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAuthenticatedUser", policy =>
        policy.RequireAuthenticatedUser());

    options.AddPolicy("RequireAdministrator", policy =>
        policy.RequireRole("Administrator"));

    options.AddPolicy("ModuleAccess", policy =>
        policy.Requirements.Add(new ModuleAccessRequirement()));

    options.AddPolicy("CanManageModules", policy =>
        policy.RequireRole("Administrator", "ModuleAdmin"));
});

// Register authorization handlers
builder.Services.AddSingleton<IAuthorizationHandler, ModuleAccessHandler>();

// Configure data protection for distributed scenarios
builder.Services.AddDataProtection()
    .PersistKeysToDbContext<ApplicationDbContext>()
    .SetApplicationName("BlazorShell");

// Configure session state for complex state management
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

// Add HTTP context accessor for service layer
builder.Services.AddHttpContextAccessor();

// Configure CORS if needed for external API access
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultPolicy", policy =>
    {
        policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "https://localhost" })
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Add SignalR with Azure SignalR Service support (optional)
//if (!string.IsNullOrEmpty(builder.Configuration["Azure:SignalR:ConnectionString"]))
//{
//    builder.Services.AddSignalR().AddAzureSignalR(builder.Configuration["Azure:SignalR:ConnectionString"]);
//}

// Add health checks
//builder.Services.AddHealthChecks()
//    .AddDbContextCheck<ApplicationDbContext>();

// Configure Antiforgery
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.SuppressXFrameOptionsHeader = false;
});

// TEMPORARY: Register Admin module services directly for testing
// Remove this once dynamic module service registration is working
//builder.Services.AddScoped<BlazorShell.Modules.Admin.Services.IModuleManagementService,
//                             BlazorShell.Modules.Admin.Services.ModuleManagementService>();
//builder.Services.AddScoped<BlazorShell.Modules.Admin.Services.IUserManagementService,
//                             BlazorShell.Modules.Admin.Services.UserManagementService>();
//builder.Services.AddScoped<BlazorShell.Modules.Admin.Services.IAuditService,
//                             BlazorShell.Modules.Admin.Services.AuditService>();
builder.Services.AddScoped<IdentityRedirectManager>();
var app = builder.Build();
// Initialize modules BEFORE configuring the pipeline
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        // NEW: Use lazy loader for initialization
        var lazyLoader = services.GetRequiredService<ILazyModuleLoader>();
        var moduleRegistry = services.GetRequiredService<IModuleRegistry>();

        // Set loading strategy based on environment
        var loadingStrategy = app.Environment.IsDevelopment()
            ? ModuleLoadingStrategy.PreloadCore  // In dev, only load core modules
            : ModuleLoadingStrategy.OnDemand;     // In prod, pure lazy loading

        lazyLoader.SetModuleLoadingStrategy(loadingStrategy);

        // Only preload absolutely essential modules
        logger.LogInformation("Initializing with lazy loading strategy: {Strategy}", loadingStrategy);

        // Load ONLY the Admin module at startup (it's required for module management)
        await lazyLoader.LoadModuleOnDemandAsync("Admin");

        // Optionally preload Dashboard if it's your default landing page
        if (app.Environment.IsDevelopment())
        {
            await lazyLoader.LoadModuleOnDemandAsync("Dashboard");
        }

        logger.LogInformation("Lazy initialization complete. Loaded modules: {Count}",
            moduleRegistry.GetModules().Count());

        // Start hot reload monitoring in development
        if (app.Environment.IsDevelopment())
        {
            var hotReload = services.GetRequiredService<IModuleHotReloadService>();
            var modules = moduleRegistry.GetModules();

            foreach (var module in modules)
            {
                var assemblyPath = module.GetType().Assembly.Location;
                if (!string.IsNullOrEmpty(assemblyPath))
                {
                    await hotReload.StartWatchingAsync(module.Name, assemblyPath);
                    logger.LogInformation("Hot reload watching enabled for {Module}", module.Name);
                }
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error during lazy initialization");
        throw;
    }
}

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    app.UseResponseCompression();
}
else
{
    app.UseDeveloperExceptionPage();
}

// Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "SAMEORIGIN");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");

    // Content Security Policy - adjust as needed
    if (!app.Environment.IsDevelopment())
    {
        context.Response.Headers.Add("Content-Security-Policy",
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
            "style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data: https:; " +
            "font-src 'self' data:; " +
            "connect-src 'self' wss: https:;");
    }

    await next();
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.UseCors("DefaultPolicy");

app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(GetModuleAssemblies(app.Services).ToArray());

app.MapAdditionalIdentityEndpoints(); // Your identity endpoints

app.MapRazorPages();
//app.MapHealthChecks("/health");

// Initialize application
await InitializeApplication(app);

app.Run();

async Task InitializeModulesEarly(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;

    try
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Starting early module initialization");

        // Initialize modules first - this ensures routes are available
        var moduleLoader = services.GetRequiredService<IModuleLoader>();
        await moduleLoader.InitializeModulesAsync();

        logger.LogInformation("Early module initialization completed");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error during early module initialization");
    }
}
// Add this helper method
IEnumerable<Assembly> GetModuleAssemblies(IServiceProvider services)
{
    var assemblies = new List<Assembly>();

    try
    {
        using var scope = services.CreateScope();
        var moduleRegistry = scope.ServiceProvider.GetService<IModuleRegistry>();
        var lazyLoader = scope.ServiceProvider.GetService<ILazyModuleLoader>();

        if (moduleRegistry != null)
        {
            // Get currently loaded modules
            var modules = moduleRegistry.GetModules();
            foreach (var module in modules)
            {
                var assembly = module.GetType().Assembly;
                if (!assemblies.Contains(assembly))
                {
                    assemblies.Add(assembly);
                }
            }

            // Important: Also add assemblies that might be lazy-loaded later
            // This ensures routes are recognized even if modules aren't loaded yet
            if (lazyLoader != null)
            {
                var allStatuses = lazyLoader.GetAllModuleStatuses();
                // We still need to register the assemblies for routing
                // even if modules aren't loaded yet
            }
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetService<ILogger<Program>>();
        logger?.LogError(ex, "Error getting module assemblies");
    }

    return assemblies;
}
// Update your existing InitializeApplication method
async Task InitializeApplication(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;

    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();

        // Apply migrations
        if (app.Environment.IsProduction())
        {
            await context.Database.MigrateAsync();
        }
        else
        {
            await context.Database.EnsureCreatedAsync();
        }

        // Seed roles
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
        await SeedRoles(roleManager);

        // Seed default admin user (only in development)
        if (app.Environment.IsDevelopment())
        {
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            await SeedDefaultAdmin(userManager, roleManager);
        }

        // REMOVED: Module loading - it's handled by lazy loader now

        // Log module status
        var moduleRegistry = services.GetRequiredService<IModuleRegistry>();
        var lazyLoader = services.GetRequiredService<ILazyModuleLoader>();
        var logger = services.GetRequiredService<ILogger<Program>>();

        var loadedCount = moduleRegistry.GetModules().Count();
        var totalCount = lazyLoader.GetAllModuleStatuses().Count();

        logger.LogInformation(
            "Application initialized with {LoadedCount}/{TotalCount} modules loaded (lazy loading enabled)",
            loadedCount, totalCount);

        // Start background cleanup service
        if (services.GetService<IHostedService>() is ModuleCleanupService cleanupService)
        {
            logger.LogInformation("Module cleanup service started");
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while initializing the application");
        throw;
    }
}
async Task SeedRoles(RoleManager<ApplicationRole> roleManager)
{
    var roles = new[]
    {
        new ApplicationRole { Name = "Administrator", Description = "Full system access", IsSystemRole = true },
        new ApplicationRole { Name = "ModuleAdmin", Description = "Can manage modules", IsSystemRole = true },
        new ApplicationRole { Name = "User", Description = "Standard user access", IsSystemRole = true }
    };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role.Name))
        {
            await roleManager.CreateAsync(role);
        }
    }
}

async Task SeedDefaultAdmin(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
{
    const string adminEmail = "admin@blazorshell.local";
    const string adminPassword = "Admin@123456";

    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true,
            FullName = "System Administrator",
            CreatedDate = DateTime.UtcNow,
            IsActive = true
        };

        var result = await userManager.CreateAsync(adminUser, adminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Administrator");
        }
    }
}

// Module registration for Autofac - NO CONSTRUCTOR PARAMETERS
public class CoreServicesModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // Register core services with proper lifetimes
        builder.RegisterType<ModuleLoader>().As<IModuleLoader>().SingleInstance();
        builder.RegisterType<ModuleRegistry>().As<IModuleRegistry>().SingleInstance();
        builder.RegisterType<NavigationService>().As<INavigationService>().SingleInstance();
        builder.RegisterType<StateContainer>().As<IStateContainer>().InstancePerLifetimeScope();
        builder.RegisterType<ModuleAuthorizationService>().As<IModuleAuthorizationService>().InstancePerLifetimeScope();
        builder.RegisterType<PluginAssemblyLoader>().As<IPluginAssemblyLoader>().SingleInstance();
        builder.RegisterType<ModuleRouteProvider>().AsSelf().SingleInstance();
        builder.RegisterType<ModuleServiceManager>().AsSelf().SingleInstance();
        builder.RegisterType<ModuleMetadataCache>().AsSelf().SingleInstance();

        builder.RegisterType<DynamicRouteService>()
            .As<IDynamicRouteService>()
            .SingleInstance();
        builder.RegisterType<LazyModuleLoader>()
    .As<ILazyModuleLoader>()
    .SingleInstance();

        builder.RegisterType<ModuleHotReloadService>()
            .As<IModuleHotReloadService>()
            .As<IHostedService>()  // Register as hosted service for auto-start
            .SingleInstance();
        builder.RegisterType<ModulePerformanceMonitor>()
      .As<IModulePerformanceMonitor>()
      .SingleInstance();
        builder.RegisterType<ModuleCleanupService>()
    .As<IHostedService>()
    .SingleInstance();
        // Note: ModuleServiceProvider is registered in the ConfigureContainer method above
        // because it needs access to the serviceCollection variable
    }
}
