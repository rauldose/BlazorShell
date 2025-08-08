using BlazorShell.Domain.Entities;

namespace BlazorShell.Application.Interfaces.Repositories;

public interface IUserRepository
{
    Task<List<ApplicationUser>> GetUsersAsync(int skip, int take);
}
