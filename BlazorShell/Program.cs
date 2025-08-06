using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Authorization;
using BlazorShell.Infrastructure.Data;
using BlazorShell.Core.Services;
using BlazorShell.Core.Interfaces;
using BlazorShell.Core.Entities;
using BlazorShell.Infrastructure.Services;
using BlazorShell.Infrastructure.Security;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using System.Reflection;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.ResponseCompression;
using BlazorShell.Components;
using Module = Autofac.Module;
using BlazorShell.Components.Account;
using IdentityRevalidatingAuthenticationStateProvider = BlazorShell.Components.Account.IdentityRevalidatingAuthenticationStateProvider;

var builder = WebApplication.CreateBuilder(args);

// Configure Autofac as DI container for plugin support
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
{
    // Register core services
    containerBuilder.RegisterModule(new CoreServicesModule());

    // Register infrastructure services
    containerBuilder.RegisterModule(new InfrastructureModule());

    // Plugin registration will happen here
    containerBuilder.RegisterModule(new PluginModule(builder.Configuration));
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
    //options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(3);
    //options.DisconnectedCircuitMaxRetained = 100;
    //options.JSInteropDefaultCallTimeout = TimeSpan.FromMinutes(1);
    //options.MaxBufferedUnacknowledgedRenderBatches = 10;
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
builder.Services.AddScoped<IdentityRedirectManager>();

var app = builder.Build();

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
    .AddInteractiveServerRenderMode();

app.MapRazorPages();
app.MapHealthChecks("/health");

// Initialize application
await InitializeApplication(app);

app.Run();

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

        // Load and initialize modules
        var moduleLoader = services.GetRequiredService<IModuleLoader>();
        await moduleLoader.InitializeModulesAsync();

        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Application initialized successfully");
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

// Module registration for Autofac
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
    }
}

public class InfrastructureModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // Register infrastructure services
        builder.RegisterType<EmailService>().As<IEmailService>().InstancePerLifetimeScope();
        builder.RegisterType<FileStorageService>().As<IFileStorageService>().InstancePerLifetimeScope();
        builder.RegisterType<CacheService>().As<ICacheService>().SingleInstance();
    }
}

public class PluginModule : Module
{
    private readonly IConfiguration _configuration;

    public PluginModule(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    protected override void Load(ContainerBuilder builder)
    {
        // Plugin registration will be implemented in Phase 2
        // This will dynamically load assemblies and register their services
    }
}