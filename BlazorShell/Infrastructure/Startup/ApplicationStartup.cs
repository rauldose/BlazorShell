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
using BlazorShell.Components;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.ResponseCompression;
using BlazorShell.Components.Account;
using IdentityRevalidatingAuthenticationStateProvider = BlazorShell.Components.Account.IdentityRevalidatingAuthenticationStateProvider;

namespace BlazorShell.Infrastructure.Startup;

public class ApplicationStartup
{
    public static void ConfigureServices(WebApplicationBuilder builder)
    {
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
                return new ModuleServiceProvider(rootProvider, builder.Services, logger);
            })
            .As<IModuleServiceProvider>()
            .SingleInstance();
        });

        ConfigureFrameworkServices(builder);
        ConfigureDatabase(builder);
        ConfigureIdentity(builder);
        ConfigureAuthentication(builder);
        ConfigureAuthorization(builder);
        ConfigureAdditionalServices(builder);
    }

    private static void ConfigureFrameworkServices(WebApplicationBuilder builder)
    {
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
    }

    private static void ConfigureDatabase(WebApplicationBuilder builder)
    {
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
    }

    private static void ConfigureIdentity(WebApplicationBuilder builder)
    {
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
    }

    private static void ConfigureAuthentication(WebApplicationBuilder builder)
    {
        // Add cascading authentication state (new .NET 8 pattern)
        builder.Services.AddCascadingAuthenticationState();

        // Configure authentication with security stamp validation
        builder.Services.AddScoped<IHostEnvironmentAuthenticationStateProvider>(sp =>
            sp.GetRequiredService<IdentityRevalidatingAuthenticationStateProvider>());
        builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
            sp.GetRequiredService<IdentityRevalidatingAuthenticationStateProvider>());
        builder.Services.AddScoped<IdentityRevalidatingAuthenticationStateProvider>();
        builder.Services.AddScoped<IdentityRedirectManager>();
    }

    private static void ConfigureAuthorization(WebApplicationBuilder builder)
    {
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
    }

    private static void ConfigureAdditionalServices(WebApplicationBuilder builder)
    {
        // Register dynamic route service
        builder.Services.AddSingleton<IDynamicRouteService, DynamicRouteService>();

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
    }
}