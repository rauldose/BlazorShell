using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;
using BlazorShell.Core.Entities;

namespace BlazorShell.Infrastructure.Security
{
    /// <summary>
    /// Builds authorization policies for module access using the policy name format
    /// ModuleAccess:ModuleName[:Permission]
    /// </summary>
    public class ModuleAuthorizationPolicyProvider : IAuthorizationPolicyProvider
    {
        private readonly DefaultAuthorizationPolicyProvider _fallbackPolicyProvider;

        public ModuleAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options)
        {
            _fallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
        }

        public Task<AuthorizationPolicy> GetDefaultPolicyAsync() =>
            _fallbackPolicyProvider.GetDefaultPolicyAsync();

        public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() =>
            _fallbackPolicyProvider.GetFallbackPolicyAsync();

        public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            if (policyName.StartsWith("ModuleAccess", StringComparison.OrdinalIgnoreCase))
            {
                var parts = policyName.Split(':', StringSplitOptions.RemoveEmptyEntries);
                var moduleName = parts.Length > 1 ? parts[1] : null;
                PermissionType? permission = null;

                if (parts.Length > 2 && Enum.TryParse<PermissionType>(parts[2], true, out var parsed))
                {
                    permission = parsed;
                }

                var policy = new AuthorizationPolicyBuilder()
                    .AddRequirements(new ModuleAccessRequirement(moduleName, permission))
                    .Build();

                return Task.FromResult<AuthorizationPolicy?>(policy);
            }

            return _fallbackPolicyProvider.GetPolicyAsync(policyName);
        }
    }
}
