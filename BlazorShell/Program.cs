using BlazorShell.Infrastructure.Startup;
using BlazorShell.Components;

var builder = WebApplication.CreateBuilder(args);

// Configure all services through dedicated startup class
ApplicationStartup.ConfigureServices(builder);

var app = builder.Build();

// Initialize modules before configuring the pipeline
await ModuleBootstrapper.InitializeModulesAsync(app);

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
    .AddAdditionalAssemblies(ModuleBootstrapper.GetModuleAssemblies(app.Services).ToArray());

app.MapAdditionalIdentityEndpoints(); // Your identity endpoints

app.MapRazorPages();
app.MapHealthChecks("/health");

// Initialize application
await ApplicationInitializer.InitializeAsync(app);

app.Run();