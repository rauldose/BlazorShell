// BlazorShell.Modules.Admin/Services/UserManagementService.cs
using Microsoft.AspNetCore.Identity;
using BlazorShell.Domain.Entities;
using BlazorShell.Domain.Repositories;
using Microsoft.Extensions.Logging;
using BlazorShell.Modules.Admin.Services.Models;
using BlazorShell.Modules.Admin.Services.Interfaces;
using BlazorShell.Domain.Events;

namespace BlazorShell.Modules.Admin.Services.Implementations
{
    public class UserManagementService : IUserManagementService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IUserRepository _userRepository;
        private readonly IDomainEventDispatcher _eventDispatcher;
        private readonly ILogger<UserManagementService> _logger;

        public UserManagementService(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IUserRepository userRepository,
            IDomainEventDispatcher eventDispatcher,
            ILogger<UserManagementService> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _userRepository = userRepository;
            _eventDispatcher = eventDispatcher;
            _logger = logger;
        }

        public async Task<IEnumerable<UserInfo>> GetUsersAsync(int page = 1, int pageSize = 20)
        {
            var users = await _userRepository.GetUsersAsync((page - 1) * pageSize, pageSize);

            var userInfos = new List<UserInfo>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userInfos.Add(new UserInfo
                {
                    Id = user.Id,
                    UserName = user.UserName ?? "",
                    Email = user.Email ?? "",
                    FullName = user.FullName ?? "",
                    IsActive = user.IsActive,
                    EmailConfirmed = user.EmailConfirmed,
                    CreatedDate = user.CreatedDate,
                    LastLoginDate = user.LastLoginDate,
                    Roles = roles.ToList()
                });
            }

            return userInfos;
        }

        public async Task<UserInfo?> GetUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return null;

            var roles = await _userManager.GetRolesAsync(user);
            return new UserInfo
            {
                Id = user.Id,
                UserName = user.UserName ?? "",
                Email = user.Email ?? "",
                FullName = user.FullName ?? "",
                IsActive = user.IsActive,
                EmailConfirmed = user.EmailConfirmed,
                CreatedDate = user.CreatedDate,
                LastLoginDate = user.LastLoginDate,
                Roles = roles.ToList()
            };
        }

        public async Task<UserOperationResult> CreateUserAsync(CreateUserModel model)
        {
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                if (model.Roles?.Any() == true)
                {
                    await _userManager.AddToRolesAsync(user, model.Roles);
                }

                await _eventDispatcher.DispatchAsync(new UserCreatedEvent(user.Id));
                return new UserOperationResult
                {
                    Success = true,
                    Message = "User created successfully",
                    UserId = user.Id
                };
            }

            return new UserOperationResult
            {
                Success = false,
                Message = string.Join(", ", result.Errors.Select(e => e.Description))
            };
        }

        public async Task<UserOperationResult> UpdateUserAsync(string userId, UpdateUserModel model)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return new UserOperationResult
                {
                    Success = false,
                    Message = "User not found"
                };
            }

            user.FullName = model.FullName;
            user.Email = model.Email;
            user.UserName = model.Email;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                return new UserOperationResult
                {
                    Success = true,
                    Message = "User updated successfully",
                    UserId = user.Id
                };
            }

            return new UserOperationResult
            {
                Success = false,
                Message = string.Join(", ", result.Errors.Select(e => e.Description))
            };
        }

        public async Task<UserOperationResult> DeleteUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return new UserOperationResult
                {
                    Success = false,
                    Message = "User not found"
                };
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                return new UserOperationResult
                {
                    Success = true,
                    Message = "User deleted successfully"
                };
            }

            return new UserOperationResult
            {
                Success = false,
                Message = string.Join(", ", result.Errors.Select(e => e.Description))
            };
        }

        public async Task<UserOperationResult> ToggleUserStatusAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return new UserOperationResult
                {
                    Success = false,
                    Message = "User not found"
                };
            }

            user.IsActive = !user.IsActive;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                return new UserOperationResult
                {
                    Success = true,
                    Message = $"User {(user.IsActive ? "activated" : "deactivated")} successfully",
                    UserId = user.Id
                };
            }

            return new UserOperationResult
            {
                Success = false,
                Message = string.Join(", ", result.Errors.Select(e => e.Description))
            };
        }

        public async Task<IEnumerable<string>> GetUserRolesAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return Enumerable.Empty<string>();

            return await _userManager.GetRolesAsync(user);
        }

        public async Task<UserOperationResult> UpdateUserRolesAsync(string userId, List<string> roles)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return new UserOperationResult
                {
                    Success = false,
                    Message = "User not found"
                };
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

            if (roles.Any())
            {
                await _userManager.AddToRolesAsync(user, roles);
            }

            return new UserOperationResult
            {
                Success = true,
                Message = "User roles updated successfully",
                UserId = user.Id
            };
        }
    }

}
