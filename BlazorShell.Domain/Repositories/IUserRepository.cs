using BlazorShell.Domain.Entities;

namespace BlazorShell.Domain.Repositories;

public interface IUserRepository
{
    Task<List<ApplicationUser>> GetUsersAsync(int skip, int take);
}
