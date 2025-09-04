using Microsoft.EntityFrameworkCore;
using ScrumApplication.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class UserRoleRepository : IUserRoleRepository
{
    private readonly ScrumDbContext _context;

    public UserRoleRepository(ScrumDbContext context)
    {
        _context = context;
    }

    public async Task<List<string>> GetUserIdsInRoleAsync(string roleId)
    {
        return await _context.UserRoles
            .Where(ur => ur.RoleId == roleId)
            .Select(ur => ur.UserId)
            .ToListAsync();
    }

    public async Task<List<string>> GetUserIdsNotInRolesAsync(List<string> excludedRoleUserIds)
    {
        return await _context.Users
            .Where(u => !excludedRoleUserIds.Contains(u.Id))
            .Select(u => u.Id)
            .ToListAsync();
    }
}
