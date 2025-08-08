using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Authorization;
using BlazorShell.Infrastructure.Data;
using BlazorShell.Application.Services;
using BlazorShell.Application.Interfaces;
using BlazorShell.Application.Interfaces.Repositories;
using BlazorShell.Infrastructure.Repositories;
using BlazorShell.Domain.Entities;
using BlazorShell.Infrastructure.Services;
using BlazorShell.Infrastructure.Security;
using BlazorShell.Components;
using BlazorShell.Services;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using System.Reflection;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.ResponseCompression;
using BlazorShell.Infrastructure.Middleware;
using Autofac.Core;
using BlazorShell.Components.Account;
using IdentityRevalidatingAuthenticationStateProvider = BlazorShell.Components.Account.IdentityRevalidatingAuthenticationStateProvider;

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
    containerBuilder.RegisterModule(new InfrastructureModule());

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

// Configure Entity Framework with connection resiliency
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
            sqlOptions.CommandTimeout(30);
        });

    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

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

// Unified authorization services
builder.Services.AddScoped<IModuleAuthorizationService, UnifiedAuthorizationService>();
builder.Services.AddScoped<IPageAuthorizationService, UnifiedAuthorizationService>();

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

// Application services
builder.Services.AddScoped<IIdentityService, IdentityService>();
builder.Services.AddScoped<ISettingsService, SettingsService>();

// Repositories
builder.Services.AddScoped<IModuleRepository, ModuleRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

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
if (!string.IsNullOrEmpty(builder.Configuration["Azure:SignalR:ConnectionString"]))
{
    builder.Services.AddSignalR().AddAzureSignalR(builder.Configuration["Azure:SignalR:ConnectionString"]);
}

// Add health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>();

// Configure Antiforgery
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.SuppressXFrameOptionsHeader = false;
});

// TEMPORARY: Register Admin module services directly for testing
// Remove this once dynamic module service registration is working
//builder.Services.AddScoped<BlazorShell.Modules.Admin.Services.Interfaces.IModuleManagementService,
//                             BlazorShell.Modules.Admin.Services.Implementations.ModuleManagementService>();
//builder.Services.AddScoped<BlazorShell.Modules.Admin.Services.Interfaces.IUserManagementService,
//                             BlazorShell.Modules.Admin.Services.Implementations.UserManagementService>();
//builder.Services.AddScoped<BlazorShell.Modules.Admin.Services.Interfaces.IAuditService,
//                             BlazorShell.Modules.Admin.Services.Implementations.AuditService>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<IModuleInitializationService, ModuleInitializationService>();
builder.Services.AddScoped<IApplicationInitializationService, ApplicationInitializationService>();
var app = builder.Build();
// Initialize modules BEFORE configuring the pipeline
using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<IModuleInitializationService>();
    await initializer.InitializeAsync();
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
app.MapHealthChecks("/health");

// Initialize application
using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<IApplicationInitializationService>();
    await initializer.InitializeAsync();
}

app.Run();
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
