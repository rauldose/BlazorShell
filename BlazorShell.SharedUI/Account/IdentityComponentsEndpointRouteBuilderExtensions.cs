using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using BlazorShell.Components.Account.Pages;
using BlazorShell.Components.Account.Pages.Manage;
using BlazorShell.Core.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing;

public static class IdentityComponentsEndpointRouteBuilderExtensions
{
    // These endpoints are required by the Identity Razor components defined in the /Components/Account/Pages directory of this project.
    public static IEndpointConventionBuilder MapAdditionalIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var accountGroup = endpoints.MapGroup("/Account");

        accountGroup.MapPost("/PerformExternalLogin", (
            HttpContext context,
            [FromServices] SignInManager<ApplicationUser> signInManager,
            [FromForm] string provider,
            [FromForm] string returnUrl) =>
        {
            IEnumerable<KeyValuePair<string, StringValues>> query = [
                new("ReturnUrl", returnUrl),
                new("Action", ExternalLogin.LoginCallbackAction)];

            var redirectUrl = UriHelper.BuildRelative(
                context.Request.PathBase,
                "/Account/ExternalLogin",
                QueryString.Create(query));

            var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return TypedResults.Challenge(properties, [provider]);
        });

        // FIX: Updated Logout endpoint to prevent redirect loops
        accountGroup.MapPost("/Logout", async (
            HttpContext httpContext,
            SignInManager<ApplicationUser> signInManager) =>
        {
            // Sign out from Identity
            await signInManager.SignOutAsync();

            // FIX: Always redirect to home page after logout
            // Never accept returnUrl for logout to prevent redirect loops
            return TypedResults.LocalRedirect("~/");
        });

        // Create manage endpoints
        var manageGroup = endpoints.MapGroup("/Account/Manage").RequireAuthorization();

        manageGroup.MapPost("/LinkExternalLogin", async (
            HttpContext context,
            [FromServices] SignInManager<ApplicationUser> signInManager,
            [FromForm] string provider) =>
        {
            // Clear the existing external cookie to ensure a clean login process
            await context.SignOutAsync(IdentityConstants.ExternalScheme);

            var redirectUrl = UriHelper.BuildRelative(
                context.Request.PathBase,
                "/Account/Manage/ExternalLogins",
                QueryString.Create("Action", ExternalLogins.LinkLoginCallbackAction));

            var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return TypedResults.Challenge(properties, [provider]);
        });

        //manageGroup.MapPost("/RemoveLogin", async (
        //    HttpContext context,
        //    [FromServices] SignInManager<ApplicationUser> signInManager,
        //    [FromForm] string loginProvider,
        //    [FromForm] string providerKey) =>
        //{
        //    var user = await signInManager.GetUserAsync(context.User);
        //    if (user == null)
        //    {
        //        return TypedResults.NotFound();
        //    }

        //    var result = await signInManager.UserManager.RemoveLoginAsync(user, loginProvider, providerKey);
        //    if (!result.Succeeded)
        //    {
        //        return TypedResults.Problem("The external login was not removed.");
        //    }

        //    await signInManager.RefreshSignInAsync(user);
        //    return TypedResults.LocalRedirect("~/Account/Manage/ExternalLogins");
        //});

        manageGroup.MapPost("/DownloadPersonalData", async (
            HttpContext context,
            [FromServices] UserManager<ApplicationUser> userManager,
            [FromServices] AuthenticationStateProvider authenticationStateProvider) =>
        {
            var user = await userManager.GetUserAsync(context.User);
            if (user == null)
            {
                return Results.NotFound($"Unable to load user with ID '{userManager.GetUserId(context.User)}'.");
            }

            var userId = await userManager.GetUserIdAsync(user);
            var personalData = new Dictionary<string, string>();
            var personalDataProps = typeof(ApplicationUser).GetProperties().Where(
                prop => Attribute.IsDefined(prop, typeof(PersonalDataAttribute)));
            foreach (var p in personalDataProps)
            {
                personalData.Add(p.Name, p.GetValue(user)?.ToString() ?? "null");
            }

            var logins = await userManager.GetLoginsAsync(user);
            foreach (var l in logins)
            {
                personalData.Add($"{l.LoginProvider} external login provider key", l.ProviderKey);
            }

            personalData.Add("Authenticator Key", (await userManager.GetAuthenticatorKeyAsync(user))!);
            var fileBytes = JsonSerializer.SerializeToUtf8Bytes(personalData);

            context.Response.Headers.TryAdd("Content-Disposition", "attachment; filename=PersonalData.json");
            return TypedResults.File(fileBytes, contentType: "application/json", fileDownloadName: "PersonalData.json");
        });

        return accountGroup;
    }
}