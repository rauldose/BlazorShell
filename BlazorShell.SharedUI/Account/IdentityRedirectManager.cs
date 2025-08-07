using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;

namespace BlazorShell.Components.Account;

public sealed class IdentityRedirectManager(NavigationManager navigationManager, IHttpContextAccessor httpContextAccessor)
{
    public const string StatusCookieName = "Identity.StatusMessage";

    private static readonly CookieBuilder StatusCookieBuilder = new()
    {
        SameSite = SameSiteMode.Strict,
        HttpOnly = true,
        IsEssential = true,
        MaxAge = TimeSpan.FromSeconds(5),
    };

    // FIX: Made this method not throw exceptions for Blazor Server compatibility
    public void RedirectTo(string? uri, bool forceLoad = false)
    {
        uri ??= "";

        // Prevent open redirects.
        if (!Uri.IsWellFormedUriString(uri, UriKind.Relative))
        {
            uri = navigationManager.ToBaseRelativePath(uri);
        }

        try
        {
            // FIX: Use forceLoad for authentication state changes
            navigationManager.NavigateTo(uri, forceLoad);
        }
        catch (NavigationException)
        {
            // FIX: NavigationException is expected during static rendering
            // It's handled by the framework as a redirect
            // For Blazor Server, the navigation still occurs
        }
    }

    public void RedirectTo(string? uri, Dictionary<string, object?> queryParameters, bool forceLoad = false)
    {
        var uriWithoutQuery = navigationManager.ToAbsoluteUri(uri).GetLeftPart(UriPartial.Path);
        var newUri = navigationManager.GetUriWithQueryParameters(uriWithoutQuery, queryParameters);
        RedirectTo(newUri, forceLoad);
    }

    public void RedirectToWithStatus(string uri, string message, HttpContext? context)
    {
        if (context != null)
        {
            context.Response.Cookies.Append(StatusCookieName, message, StatusCookieBuilder.Build(context));
        }
        RedirectTo(uri);
    }

    private string CurrentPath => navigationManager.ToAbsoluteUri(navigationManager.Uri).GetLeftPart(UriPartial.Path);

    public void RedirectToCurrentPage() => RedirectTo(CurrentPath);

    public void RedirectToCurrentPageWithStatus(string message, HttpContext? context)
        => RedirectToWithStatus(CurrentPath, message, context);

    // FIX: Add method for login redirects with forceLoad
    public void RedirectToLogin(string? returnUrl = null)
    {
        var uri = "/Account/Login";
        if (!string.IsNullOrEmpty(returnUrl))
        {
            var parameters = new Dictionary<string, object?> { ["returnUrl"] = returnUrl };
            uri = navigationManager.GetUriWithQueryParameters(uri, parameters);
        }
        RedirectTo(uri, forceLoad: true);
    }

    // FIX: Add method for post-logout redirect
    public void RedirectToHome()
    {
        RedirectTo("/", forceLoad: true);
    }
}