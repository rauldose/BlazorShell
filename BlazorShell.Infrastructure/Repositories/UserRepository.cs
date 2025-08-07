using BlazorShell.Domain.Entities;
using BlazorShell.Domain.Repositories;
using BlazorShell.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BlazorShell.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _dbContext;

    public UserRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<ApplicationUser>> GetUsersAsync(int skip, int take) =>
        await _dbContext.Users
            .OrderBy(u => u.UserName)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
}
