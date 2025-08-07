using BlazorShell.Domain.Entities;
using Microsoft.AspNetCore.Authorization;

namespace BlazorShell.Infrastructure.Security;

public class ModuleAccessRequirement : IAuthorizationRequirement
{
    public string ModuleName { get; set; }
    public PermissionType? RequiredPermission { get; set; }

    public ModuleAccessRequirement()
    {
    }

    public ModuleAccessRequirement(string moduleName, PermissionType? requiredPermission = null)
    {
        ModuleName = moduleName;
        RequiredPermission = requiredPermission;
    }
}
