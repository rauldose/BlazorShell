using System.Security.Claims;
using BlazorShell.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorShell.Infrastructure.Security;

public class ModuleAccessHandler : AuthorizationHandler<ModuleAccessRequirement>
{
    private readonly IServiceProvider _serviceProvider;

    public ModuleAccessHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ModuleAccessRequirement requirement)
    {
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var moduleAuthService = scope.ServiceProvider.GetRequiredService<IModuleAuthorizationService>();

        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        if (!string.IsNullOrEmpty(requirement.ModuleName))
        {
            var hasAccess = await moduleAuthService.CanAccessModuleAsync(userId, requirement.ModuleName);

            if (hasAccess && requirement.RequiredPermission.HasValue)
            {
                hasAccess = await moduleAuthService.HasPermissionAsync(
                    userId,
                    requirement.ModuleName,
                    requirement.RequiredPermission.Value);
            }

            if (hasAccess)
            {
                context.Succeed(requirement);
            }
        }
        else
        {
            context.Succeed(requirement);
        }
    }
}
